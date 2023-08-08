using EPiServer.ServiceLocation;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[Options(ConfigurationSection = ConfigurationSectionConstants.CmsUI)]
public class LabsOptions
{
    public bool MigrateInlineBlocksToForThisPageFolders { get; set; } = true;

    public bool RemoveConvertedLocalBlocks { get; set; } = true;
}
