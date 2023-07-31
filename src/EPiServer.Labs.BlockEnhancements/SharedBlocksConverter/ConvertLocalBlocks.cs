using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer.Cms.Shell;
using EPiServer.Cms.Shell.UI.Rest.Capabilities;
using EPiServer.Core;
using EPiServer.Data.Entity;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[ServiceConfiguration(typeof(ConvertLocalBlocks))]
public class ConvertLocalBlocks
{
    private readonly IContentRepository _contentRepository;
    private readonly IContentVersionRepository _contentVersionRepository;
    private readonly IContentCapability _localContentCapability;
    private readonly IBlockPropertyFactory _blockPropertyFactory;
    private readonly ILanguageBranchRepository _languageBranchRepository;
    private static readonly ILogger _log = LogManager.GetLogger(typeof(ConvertLocalBlocks));

    public ConvertLocalBlocks(IContentRepository contentRepository, IEnumerable<IContentCapability> capabilities,
        IBlockPropertyFactory blockPropertyFactory, IContentVersionRepository contentVersionRepository, ILanguageBranchRepository languageBranchRepository)
    {
        _contentRepository = contentRepository;
        _blockPropertyFactory = blockPropertyFactory;
        _contentVersionRepository = contentVersionRepository;
        _languageBranchRepository = languageBranchRepository;
        _localContentCapability = capabilities.Single(x => x.Key == "isLocalContent");
    }

    public int Convert(ContentReference rootPage, out int convertedCount, Action<string> onStatusChanged = null)
    {
        IEnumerable<CultureInfo> languagesToCheck;
        var root = _contentRepository.Get<IContent>(rootPage);
        if (rootPage.WorkID != 0)
        {
            languagesToCheck = new [] { new CultureInfo(root.LanguageBranch()) };
        }
        else
        {
            var enabledLanguages = _languageBranchRepository.ListEnabled();
            languagesToCheck = enabledLanguages.Where(x => x.Enabled).Select(x => x.Culture);
        }

        var convertedContentCount = 0;
        var analyzedContentCount = 0;
        foreach (var language in languagesToCheck)
        {
            analyzedContentCount += ConvertRecursively(rootPage, root, language, out var languageConvertedCount);
            convertedContentCount += languageConvertedCount;
        }

        convertedCount = convertedContentCount;
        return analyzedContentCount;
    }

    private int ConvertRecursively(ContentReference rootPage, IContent root, CultureInfo language, out int convertedCount, Action<string> onStatusChanged = null)
    {
        var convertedContentCount = 0;
        var analyzedContentCount = 0;

        var convertedRoot = TryConvertContent(root, language);
        if (convertedRoot)
        {
            convertedContentCount++;
        }

        var children = _contentRepository.GetChildren<IContent>(rootPage, language).ToList();
        analyzedContentCount += children.Count + 1;
        foreach (var content in children)
        {
            var converted = TryConvertContent(content, language);
            if (converted)
            {
                convertedContentCount++;
            }

            analyzedContentCount += ConvertRecursively(content.ContentLink, content, language, out var convertedChildrenCount);
            convertedContentCount += convertedChildrenCount;
            onStatusChanged?.Invoke(
                $"Analyzed {analyzedContentCount} contents and converted {convertedContentCount} to inline blocks");
        }

        convertedCount = convertedContentCount;
        return analyzedContentCount;
    }

    private bool TryConvertContent(IContent content, CultureInfo culture)
    {
        if (content is ContentFolder or IContentMedia or not IReadOnly)
        {
            return false;
        }

        var convertedBlocks = new List<ContentReference>();
        var publishedOrDraft = _contentVersionRepository.LoadCommonDraft(content.ContentLink, culture.Name);
        if (publishedOrDraft == null)
        {
            return false;
        }
        var draftContent = _contentRepository.Get<IContent>(publishedOrDraft.ContentLink);
        if ((draftContent as IReadOnly).CreateWritableClone() is not IContent writableClone)
        {
            return false;
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
                convertedBlocks.AddRange(ConvertContentAreaProperty(contentAreaValue, culture));
            }
        }

        if (convertedBlocks.Any())
        {
            _contentRepository.Save(writableClone, SaveAction.Patch, AccessLevel.NoAccess);
            _log.Information(
                $"Converted local blocks on content item with contentLink = {writableClone.ContentLink}, convertedBlocksCout = {convertedBlocks.Count}");

            //TODO: option if should do force delete
            // foreach (var contentReference in blocksToDelete)
            // {
            //     if (_contentRepository.TryGet<IContent>(contentReference, out var _))
            //     {
            //         _contentRepository.Delete(contentReference, true, AccessLevel.NoAccess);
            //     }
            // }

            return true;
        }

        return false;
    }

    private IEnumerable<ContentReference> ConvertContentAreaProperty(ContentArea propertyDataValue, CultureInfo culture)
    {
        var blocksToDelete = new List<ContentReference>();
        for (var i = 0; i < propertyDataValue.Items.Count; i++)
        {
            var contentAreaItem = propertyDataValue.Items[i];
            if (contentAreaItem.InlineBlock != null || ContentReference.IsNullOrEmpty(contentAreaItem.ContentLink))
            {
                continue;
            }

            var content = _contentRepository.Get<IContent>(contentAreaItem.ContentLink, culture);
            if (content is not BlockData)
            {
                continue;
            }

            if (!_localContentCapability.IsCapable(content))
            {
                continue;
            }

            var inlineBlock = _blockPropertyFactory.CreateFromSharedInstance(content as BlockData);
            if (!inlineBlock.Property.Any() || inlineBlock.Property.All(x => x.IsNull))
            {
                continue;
            }

            // Inline blocks do not allow to store category properties
            var unsupportedProperties = inlineBlock.Property
                .Where(x => x.Type == PropertyDataType.Category)
                .Select(x => x.Name)
                .ToList();
            foreach (var propertyName in unsupportedProperties)
            {
                inlineBlock.Property.Remove(propertyName);
            }

            var writableClone = contentAreaItem.CreateWritableClone();
            writableClone.InlineBlock = inlineBlock;
            writableClone.ContentLink = null;
            propertyDataValue.Items[i] = writableClone;
            blocksToDelete.Add(content.ContentLink);
        }

        return blocksToDelete;
    }
}
