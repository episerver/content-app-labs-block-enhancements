using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Cms.Shell.UI.Rest.Capabilities;
using EPiServer.Core;
using EPiServer.Data.Entity;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[ServiceConfiguration(typeof(ConvertLocalBlocks))]
public class ConvertLocalBlocks
{
    private readonly IContentRepository _contentRepository;
    private readonly IContentCapability _localContentCapability;
    private readonly IBlockPropertyFactory _blockPropertyFactory;

    public ConvertLocalBlocks(IContentRepository contentRepository, IEnumerable<IContentCapability> capabilities,
        IBlockPropertyFactory blockPropertyFactory)
    {
        _contentRepository = contentRepository;
        _blockPropertyFactory = blockPropertyFactory;

        _localContentCapability = capabilities.Single(x => x.Key == "isLocalContent");
    }

    public int Convert(ContentReference rootPage, out int convertedCount, Action<string> onStatusChanged = null)
    {
        return ConvertRecursively(rootPage, out convertedCount);
    }

    private int ConvertRecursively(ContentReference rootPage, out int convertedCount, Action<string> onStatusChanged = null)
    {
        var convertedContentCount = 0;
        var root = _contentRepository.Get<IContent>(rootPage);
        var convertedRoot = TryConvertContent(root);
        if (convertedRoot)
        {
            convertedContentCount++;
        }
        var children = _contentRepository.GetChildren<IContent>(rootPage, LanguageSelector.AutoDetect(true)).ToList();
        var analyzedContentCount = children.Count + 1;
        foreach (var content in children)
        {
            var converted = TryConvertContent(content);
            if (converted)
            {
                convertedContentCount++;
            }

            analyzedContentCount += ConvertRecursively(content.ContentLink, out var convertedChildrenCount);
            convertedContentCount += convertedChildrenCount;
            onStatusChanged?.Invoke(
                $"Analyzed {analyzedContentCount} contents and converted {convertedContentCount} to inline blocks");
        }

        convertedCount = convertedContentCount;
        return analyzedContentCount;
    }

    private bool TryConvertContent(IContent content)
    {
        if (content is ContentFolder or IContentMedia or not IReadOnly)
        {
            return false;
        }

        var blocksToDelete = new List<ContentReference>();
        if ((content as IReadOnly).CreateWritableClone() is not IContent writableClone)
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
                blocksToDelete.AddRange(ConvertContentAreaProperty(contentAreaValue));
            }
        }

        if (blocksToDelete.Any())
        {
            _contentRepository.Save(writableClone, SaveAction.Patch, AccessLevel.NoAccess);

            foreach (var contentReference in blocksToDelete)
            {
                if (_contentRepository.TryGet<IContent>(contentReference, out var _))
                {
                    //TODO: option if should do force delete
                    _contentRepository.Delete(contentReference, true, AccessLevel.NoAccess);
                }
            }

            return true;
        }

        return false;
    }

    private IEnumerable<ContentReference> ConvertContentAreaProperty(ContentArea propertyDataValue)
    {
        var blocksToDelete = new List<ContentReference>();
        for (var i = 0; i < propertyDataValue.Items.Count; i++)
        {
            var contentAreaItem = propertyDataValue.Items[i];
            var content = contentAreaItem.LoadContent() as IContent;
            if (content is not BlockData)
            {
                continue;
            }

            if (!_localContentCapability.IsCapable(content))
            {
                continue;
            }

            var inlineBlock = _blockPropertyFactory.CreateFromSharedInstance(content as BlockData);

            // Inline blocks do not allow to store category properties
            var unsupportedProperties = inlineBlock.Property
                .Where(x => x.Type == PropertyDataType.Category)
                .Select(x => x.Name)
                .ToList();
            foreach (var propertyName in unsupportedProperties)
            {
                inlineBlock.Property.Remove(propertyName);
            }

            propertyDataValue.Items[i] = new ContentAreaItem { InlineBlock = inlineBlock };
            blocksToDelete.Add(content.ContentLink);
        }

        return blocksToDelete;
    }
}
