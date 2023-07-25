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

    public int Convert(out int convertedCount, out int convertedContentItems, Action<string> onStatusChanged = null)
    {
        var versionFilter = new VersionFilter
        {
            SortColumn = VersionSortColumn.Saved,
            SortDirection = VersionSortDirection.Descending
        };
        var latestVersions = _contentVersionRepository.List(versionFilter, 0, _labsOptions.VersionsToCheck, out var totalCount);

        var convertedBlocksCount = 0;
        var convertedParentContentItemsCount = 0;
        var analyzedContentCount = totalCount;
        var index = 1;
        foreach (var version in latestVersions)
        {
            var convertedBlocks = TryConvertContent(version);
            if (convertedBlocks.Any())
            {
                convertedBlocksCount += convertedBlocks.Count();
                convertedParentContentItemsCount++;
            }

            onStatusChanged?.Invoke(
                $"Analyzed {index} out of {analyzedContentCount} content items and converted {convertedBlocksCount} inline blocks in {convertedParentContentItemsCount}");
            index++;
        }

        convertedCount = convertedBlocksCount;
        convertedContentItems = convertedParentContentItemsCount;
        return analyzedContentCount;
    }

    private IEnumerable<ContentReference> TryConvertContent(ContentVersion version)
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
                convertedBlocks.AddRange(ConvertContentAreaProperty(writableClone, contentAreaValue));
            }
        }

        if (convertedBlocks.Any())
        {
            _contentRepository.Save(writableClone, SaveAction.Patch, AccessLevel.NoAccess);
            _log.Information(
                $"Converted inline blocks on content item with contentLink = {writableClone.ContentLink}, convertedBlocksCout = {convertedBlocks.Count}");

            return convertedBlocks;
        }

        return Enumerable.Empty<ContentReference>();
    }

    private IEnumerable<ContentReference> ConvertContentAreaProperty(IContent owner, ContentArea propertyDataValue)
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
            var forThisPageFolder = _contentChangeManager.GetOrCreateContentAssetsFolder(owner.ContentLink);
            var localBlock = _contentRepository.GetDefault<IContent>(forThisPageFolder, contentType.ID);

            foreach (var property in contentAreaItem.InlineBlock.Property)
            {
                localBlock.SetPropertyValue(property.Name, property.Value);
            }

            localBlock.Name = contentType.LocalizedName + (index + 1);
            var localBlockContentLink = _contentRepository.Save(localBlock, SaveAction.Save | SaveAction.Publish, AccessLevel.NoAccess);

            var writableClone = contentAreaItem.CreateWritableClone();
            writableClone.InlineBlock = null;
            writableClone.ContentLink = localBlockContentLink;
            convertedInlineBlocks.Add(localBlockContentLink);
            propertyDataValue.Items[index] = writableClone;
        }

        return convertedInlineBlocks;
    }
}
