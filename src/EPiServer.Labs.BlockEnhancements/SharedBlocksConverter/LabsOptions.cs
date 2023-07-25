using EPiServer.ServiceLocation;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[Options(ConfigurationSection = ConfigurationSectionConstants.CmsUI)]
public class LabsOptions
{
    public int VersionsToCheck { get; set; } = 10000;
}
