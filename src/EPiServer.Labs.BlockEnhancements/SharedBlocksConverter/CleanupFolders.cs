using System;
using System.Linq;
using EPiServer.Core;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[ServiceConfiguration(typeof(CleanupFolders))]
public class CleanupFolders
{
    private IContentRepository _contentRepository;

    public CleanupFolders(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    public int Convert( ContentReference rootPage, out int convertedCount, Action<string> onStatusChanged = null)
    {
        return ConvertRecursively(rootPage, out convertedCount);
    }

    private int ConvertRecursively(ContentReference rootPage, out int convertedCount, Action<string> onStatusChanged = null)
    {
        var convertedFoldersCount = 0;
        var children = _contentRepository.GetChildren<IContent>(rootPage, LanguageSelector.AutoDetect(true)).ToList();
        var analyzedFoldersCount = children.OfType<ContentFolder>().Count();
        foreach (var content in children)
        {
            // do not do the cleanup for external providers
            if (content.ContentLink.IsExternalProvider)
            {
                continue;
            }

            var converted = TryDeleteFolder(content);
            if (converted)
            {
                convertedFoldersCount++;
            }

            analyzedFoldersCount += ConvertRecursively(content.ContentLink, out var convertedChildrenCount);
            convertedFoldersCount += convertedChildrenCount;
            onStatusChanged?.Invoke(
                $"Analyzed {analyzedFoldersCount} folders and deleted {convertedFoldersCount}");
        }

        convertedCount = convertedFoldersCount;
        return analyzedFoldersCount;
    }

    private bool TryDeleteFolder(IContent content)
    {
        if (content is not ContentFolder)
        {
            return false;
        }

        if (content is ContentAssetFolder)
        {
            return false;
        }

        var children = _contentRepository.GetChildren<IContent>(content.ContentLink, LanguageSelector.AutoDetect(true)).ToList();
        if (children.Any())
        {
            return false;
        }

        _contentRepository.Delete(content.ContentLink, true, AccessLevel.NoAccess);

        // Try to recursively delete parent folders
        TryDeleteFolder(_contentRepository.Get<IContent>(content.ParentLink));
        return true;
    }
}
