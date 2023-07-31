using System;
using System.Linq;
using EPiServer.Cms.Shell.UI.Rest.Internal;
using EPiServer.Core;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[ServiceConfiguration(typeof(ConvertSharedBlocks))]
public class ConvertSharedBlocks
{
    private readonly IContentRepository _contentRepository;
    private readonly ReferencedContentResolver _referencedContentResolver;

    public ConvertSharedBlocks(IContentRepository contentRepository,
        ReferencedContentResolver referencedContentResolver)
    {
        _contentRepository = contentRepository;
        _referencedContentResolver = referencedContentResolver;
    }

    public int Convert(ContentReference rootPage, out int convertedCount, Action<string> onStatusChanged = null)
    {
        return ConvertRecursively(rootPage, out convertedCount, onStatusChanged);
    }

    private int ConvertRecursively(ContentReference rootPage, out int convertedCount, Action<string> onStatusChanged = null)
    {
        var convertedBlocksCount = 0;

        var children = _contentRepository.GetChildren<IContent>(rootPage, LanguageSelector.AutoDetect(true)).ToList();
        var analyzedBlocksCount = children.Count;
        foreach (var content in children)
        {
            if (content is BlockData)
            {
                var converted = TryConvertBlock(content);
                if (converted)
                {
                    convertedBlocksCount++;
                }
            }

            if (content is not ContentAssetFolder)
            {
                analyzedBlocksCount += ConvertRecursively(content.ContentLink, out var convertedChildrenCount);
                convertedBlocksCount += convertedChildrenCount;
            }

            onStatusChanged?.Invoke(
                $"Analyzed {analyzedBlocksCount} shared blocks and converted {convertedBlocksCount}");
        }

        convertedCount = convertedBlocksCount;
        return analyzedBlocksCount;
    }

    private bool TryConvertBlock(IContent content)
    {
        var references = _referencedContentResolver.GetReferenceList(content.ContentLink).ToList();
        if (references.Count != 1)
        {
            return false;
        }

        var referencedContent = references[0];

        // do not convert external content providers
        if (referencedContent.ContentLink.IsExternalProvider)
        {
            return false;
        }

        var contentAssetHelper = ServiceLocator.Current.GetInstance<ContentAssetHelper>();
        var assetsFolder = contentAssetHelper.GetOrCreateAssetFolder(referencedContent.ContentLink);

        //TODO: should we use "For this block" or parent for this page?
        _contentRepository.Move(content.ContentLink, assetsFolder.ContentLink, AccessLevel.NoAccess,
            AccessLevel.NoAccess);

        return true;
    }
}
