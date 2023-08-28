using System.Collections.Generic;
using EPiServer.Authorization;
using EPiServer.Core;
using EPiServer.Shell.Web.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[Authorize(Policy = CmsPolicyNames.CmsAdmin)]
public class SharedBlocksConverterPluginController : Controller
{
    private readonly ConvertInlineBlocks _convertInlineBlocks;
    private readonly ConvertSharedBlocks _convertSharedBlocks;
    private readonly ConvertLocalBlocks _convertLocalBlocks;
    private readonly CleanupFolders _cleanupFolders;

    public SharedBlocksConverterPluginController(ConvertSharedBlocks convertSharedBlocks,
        ConvertLocalBlocks convertLocalBlocks,
        CleanupFolders cleanupFolders,
        ConvertInlineBlocks convertInlineBlocks)
    {
        _convertSharedBlocks = convertSharedBlocks;
        _convertLocalBlocks = convertLocalBlocks;
        _cleanupFolders = cleanupFolders;
        _convertInlineBlocks = convertInlineBlocks;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Convert([FromForm] ConvertSharedBlocksDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.root))
        {
            return new JsonDataResult("Root not set");
        }

        if (!ContentReference.TryParse(dto.root, out var rootId))
        {
            return new JsonDataResult("Cannot parse root page");
        }

        var result = new List<string>();

        if (dto.convertInlineBlock)
        {
            var analyzedInlineBlocks = _convertInlineBlocks.Convert(dto.dryRun);
            result.Add(analyzedInlineBlocks.ToString());
        }

        if (dto.convertSharedBlocks)
        {
            var analyzedSharedBlocksCount = _convertSharedBlocks.Convert(rootId, out var convertedSharedBlocksCount);
            result.Add(
                $"Analyzed {analyzedSharedBlocksCount} shared blocks and converted {convertedSharedBlocksCount}");
        }

        if (dto.convertLocalBlocks)
        {
            var analyzedContentCount = _convertLocalBlocks.Convert(rootId, out var convertedContentCount);
            result.Add(
                $"Analyzed {analyzedContentCount} contents and converted {convertedContentCount} to inline blocks");
        }

        if (dto.cleanupFolders)
        {
            var analyzedFoldersCount = _cleanupFolders.Convert(rootId, out var convertedFoldersCount);
            result.Add(
                $"Analyzed {analyzedFoldersCount} folders and deleted {convertedFoldersCount}");
        }

        return new JsonDataResult(result);
    }

    public class ConvertSharedBlocksDto
    {
        public string root { get; set; }
        public bool convertSharedBlocks { get; set; }
        public bool convertLocalBlocks { get; set; }
        public bool cleanupFolders { get; set; }
        public bool convertInlineBlock { get; set; }
        public bool dryRun { get; set; }
    }
}
