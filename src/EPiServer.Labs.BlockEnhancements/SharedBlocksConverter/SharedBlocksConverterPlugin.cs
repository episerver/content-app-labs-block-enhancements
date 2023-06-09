using System.Collections.Generic;
using EPiServer.Shell;
using EPiServer.Shell.Navigation;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[MenuProvider]
public class SharedBlocksConverterMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        var pluginUrl = Paths.ToResource("episerver-labs-block-enhancements" , "SharedBlocksConverterPlugin");
        var controllerPath = $"{pluginUrl}/Index";

        var urlMenuItem1 = new UrlMenuItem("Inline block converter", MenuPaths.Global + "/cms/admin/convertSharedBlocks",
            // Paths.ToResource(GetType(), "SharedBlocksConverterPlugin/Index"))
            controllerPath)
        {
            IsAvailable = _ => true,
            SortIndex = 100,
        };

        return new List<MenuItem>(1)
        {
            urlMenuItem1
        };
    }
}
