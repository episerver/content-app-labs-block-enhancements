using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Cms.Shell;
using EPiServer.Cms.Shell.UI.Rest;
using EPiServer.Core;
using EPiServer.Data.Entity;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

public class ConvertInlineBlocksResult
{
    public int Analyzed { get; set; }
    public int ConvertedBlocksCount { get; set; }
    public int ConvertedContentItems { get; set; }
    public int FailedContentItems { get; set; }

    public override string ToString()
    {
        var result = $"Analyzed {Analyzed} content items and converted {ConvertedBlocksCount} in {ConvertedContentItems} content items. ";
        if (FailedContentItems > 0)
        {
            result +=
                $"{FailedContentItems} content items with inline blocks failed. Please see the logs for more details.";
        }
        return result;
    }
}

[ServiceConfiguration(typeof(ConvertInlineBlocks))]
public class ConvertInlineBlocks
{
    private readonly IContentRepository _contentRepository;
    private readonly IContentVersionRepository _contentVersionRepository;
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentChangeManager _contentChangeManager;
    private readonly LabsOptions _labsOptions;
    private static readonly ILogger _log = LogManager.GetLogger(typeof(ConvertInlineBlocks));

    public ConvertInlineBlocks(IContentRepository contentRepository, IContentChangeManager contentChangeManager,
        IContentTypeRepository contentTypeRepository, IContentVersionRepository contentVersionRepository, LabsOptions labsOptions)
    {
        _contentRepository = contentRepository;
        _contentChangeManager = contentChangeManager;
        _contentTypeRepository = contentTypeRepository;
        _contentVersionRepository = contentVersionRepository;
        _labsOptions = labsOptions;
    }

    public ConvertInlineBlocksResult Convert(Action<string> onStatusChanged = null)
    {
        var versionFilter = new VersionFilter
        {
            SortColumn = VersionSortColumn.Saved,
            SortDirection = VersionSortDirection.Descending
        };
        var latestVersions = _contentVersionRepository.List(versionFilter, 0, _labsOptions.VersionsToCheck, out var totalCount);

        var convertedBlocksCount = 0;
        var convertedContentItems = 0;
        var failedContentItems = 0;
        var index = 1;
        var defaultConversionFolderName = "Converted Local Blocks " + DateTime.Now.ToString("u");
        foreach (var version in latestVersions)
        {
            try
            {
                var convertedBlocks = TryConvertContent(version, defaultConversionFolderName);
                if (convertedBlocks.Any())
                {
                    convertedBlocksCount += convertedBlocks.Count();
                    convertedContentItems++;
                }
            }
            catch(Exception ex)
            {
                failedContentItems++;
                _log.Warning($"Failed to convert inline blocks on content item with contentLink = {version.ContentLink}", ex);
            }

            onStatusChanged?.Invoke(
                $"Analyzed {index} out of {totalCount}, so far converted {convertedContentItems} content items, ${failedContentItems} failed");
            index++;
        }

        return new ConvertInlineBlocksResult
        {
            Analyzed = totalCount,
            ConvertedBlocksCount = convertedBlocksCount,
            ConvertedContentItems = convertedContentItems,
            FailedContentItems = failedContentItems
        };
    }

    private IEnumerable<ContentReference> TryConvertContent(ContentVersion version, string defaultConversionFolderName)
    {
        var content = _contentRepository.Get<IContent>(version.ContentLink);
        if (content is ContentFolder or IContentMedia or not IReadOnly)
        {
            return Enumerable.Empty<ContentReference>();
        }

        var convertedBlocks = new List<ContentReference>();
        if ((content as IReadOnly).CreateWritableClone() is not IContent writableClone)
        {
            return Enumerable.Empty<ContentReference>();
        }
        foreach (var propertyData in writableClone.Property)
        {
            // ContentArea property inherits from LongString
            if (propertyData.Type != PropertyDataType.LongString)
            {
                continue;
            }

            if (propertyData.PropertyValueType == typeof(ContentArea)
                && propertyData.Value != null
                && propertyData.Value is ContentArea contentAreaValue)
            {
                convertedBlocks.AddRange(ConvertContentAreaProperty(writableClone, contentAreaValue, defaultConversionFolderName));
            }
        }

        if (convertedBlocks.Any())
        {
            _contentRepository.Save(writableClone, SaveAction.Patch, AccessLevel.NoAccess);
            _log.Information(
                $"Converted inline blocks on content item with contentLink = {writableClone.ContentLink}, convertedBlocksCount = {convertedBlocks.Count}");

            return convertedBlocks;
        }

        return Enumerable.Empty<ContentReference>();
    }

    private IEnumerable<ContentReference> ConvertContentAreaProperty(IContent owner, ContentArea propertyDataValue, string defaultConversionFolderName)
    {
        var convertedInlineBlocks = new List<ContentReference>();
        for (var index = 0; index < propertyDataValue.Items.Count; index++)
        {
            var contentAreaItem = propertyDataValue.Items[index];
            if (!ContentReference.IsNullOrEmpty(contentAreaItem.ContentLink) || contentAreaItem.InlineBlock == null)
            {
                continue;
            }

            var contentType = _contentTypeRepository.Load(contentAreaItem.InlineBlock.ContentTypeID);

            ContentReference folderToMigrateInto;
            if (_labsOptions.MigrateInlineBlocksToForThisPageFolders)
            {
                var forThisFolder = _contentChangeManager.GetOrCreateContentAssetsFolder(owner.ContentLink);
                if (forThisFolder != null)
                {
                    folderToMigrateInto = forThisFolder;
                }
                else
                {
                    folderToMigrateInto = GetOrCreateDefaultFolder(defaultConversionFolderName);
                    _log.Warning($"Could not GetOrCreate an assets folder for {owner.ContentLink}. Falling back to a global folder");
                }
            }
            else
            {
                folderToMigrateInto = GetOrCreateDefaultFolder(defaultConversionFolderName);
            }

            var localBlock = _contentRepository.GetDefault<IContent>(folderToMigrateInto, contentType.ID);

            foreach (var property in contentAreaItem.InlineBlock.Property)
            {
                localBlock.SetPropertyValue(property.Name, property.Value);
            }

            //In case all blocks are migrated to the same folder we want their names to be prefixed with their parent's name
            localBlock.Name = this._labsOptions.MigrateInlineBlocksToForThisPageFolders
                ? contentType.LocalizedName + (index + 1)
                : $"{owner.Name}_{contentType.LocalizedName}_{index + 1}";
            var localBlockContentLink = _contentRepository.Save(localBlock, SaveAction.Save | SaveAction.Publish, AccessLevel.NoAccess);

            var writableClone = contentAreaItem.CreateWritableClone();
            writableClone.InlineBlock = null;
            writableClone.ContentLink = localBlockContentLink;
            convertedInlineBlocks.Add(localBlockContentLink);
            propertyDataValue.Items[index] = writableClone;
        }

        return convertedInlineBlocks;
    }

    private ContentReference GetOrCreateDefaultFolder(string defaultConversionFolderName)
    {
        var children = _contentRepository.GetChildren<ContentFolder>(ContentReference.GlobalBlockFolder);
        var defaultFolder = children.FirstOrDefault(x =>
            x.Name.Equals(defaultConversionFolderName, StringComparison.InvariantCultureIgnoreCase));
        if (defaultFolder != null)
        {
            return defaultFolder.ContentLink;
        }

        var contentFolderType = this._contentTypeRepository.Load(typeof(ContentFolder));
        var folder = _contentRepository.GetDefault<ContentFolder>(ContentReference.GlobalBlockFolder, contentFolderType.ID);
        folder.Name = defaultConversionFolderName;
        return _contentRepository.Save(folder, SaveAction.Publish, AccessLevel.NoAccess);
    }
}
