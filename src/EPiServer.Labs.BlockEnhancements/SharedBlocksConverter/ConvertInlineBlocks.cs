using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Cms.Shell;
using EPiServer.Cms.Shell.UI.Rest;
using EPiServer.Core;
using EPiServer.Data.Entity;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
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

    public ConvertInlineBlocks(IContentRepository contentRepository, IContentChangeManager contentChangeManager,
        IContentTypeRepository contentTypeRepository, IContentVersionRepository contentVersionRepository)
    {
        _contentRepository = contentRepository;
        _contentChangeManager = contentChangeManager;
        _contentTypeRepository = contentTypeRepository;
        _contentVersionRepository = contentVersionRepository;
    }

    public int Convert(ContentReference rootPage, out int convertedCount, out int convertedContentItems, Action<string> onStatusChanged = null)
    {
        return ConvertRecursively(rootPage, out convertedCount, out convertedContentItems, onStatusChanged);
    }

    private int ConvertRecursively(ContentReference rootPage, out int convertedCount, out int convertedContentItems, Action<string> onStatusChanged = null)
    {
        var convertedBlocksCount = 0;
        var convertedParentContentItemsCount = 0;
        var root = _contentRepository.Get<IContent>(rootPage);
        var convertedRoot = TryConvertContent(root);
        if (convertedRoot.Any())
        {
            convertedBlocksCount += convertedRoot.Count();
        }
        var children = _contentRepository.GetChildren<IContent>(rootPage, LanguageSelector.AutoDetect(true)).ToList();
        var analyzedContentCount = children.Count + 1;
        foreach (var content in children)
        {
            var convertedBlocks = TryConvertContent(content);
            if (convertedBlocks.Any())
            {
                convertedBlocksCount += convertedBlocks.Count();
                convertedParentContentItemsCount++;
            }

            analyzedContentCount += ConvertRecursively(content.ContentLink, out var convertedChildrenCount, out var convertedContentItemsCount);
            convertedBlocksCount += convertedChildrenCount;
            convertedParentContentItemsCount += convertedContentItemsCount;
            onStatusChanged?.Invoke(
                $"Analyzed {analyzedContentCount} content items and converted {convertedBlocksCount} inline blocks in {convertedParentContentItemsCount}");
        }

        convertedCount = convertedBlocksCount;
        convertedContentItems = convertedParentContentItemsCount;
        return analyzedContentCount;
    }

    private IEnumerable<ContentReference> TryConvertContent(IContent content)
    {
        if (content is ContentFolder or IContentMedia or not IReadOnly)
        {
            return Enumerable.Empty<ContentReference>();
        }

        var publishedOrDraft = _contentVersionRepository.LoadCommonDraft(content.ContentLink, content.LanguageBranch());
        var draftContent = _contentRepository.Get<IContent>(publishedOrDraft.ContentLink);

        var convertedBlocks = new List<ContentReference>();
        if ((draftContent as IReadOnly).CreateWritableClone() is not IContent writableClone)
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
                convertedBlocks.AddRange(ConvertContentAreaProperty(draftContent, contentAreaValue));
            }
        }

        if (convertedBlocks.Any())
        {
            _contentRepository.Save(writableClone, SaveAction.Patch, AccessLevel.NoAccess);

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
