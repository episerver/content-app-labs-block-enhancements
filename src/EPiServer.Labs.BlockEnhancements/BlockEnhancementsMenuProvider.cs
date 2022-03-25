using System.Collections.Generic;
using System.Linq;
using EPiServer.Authorization;
using EPiServer.Shell;
using EPiServer.Shell.Navigation;

namespace EPiServer.Labs.BlockEnhancements
{
    [MenuProvider]
    public class BlockEnhancementsMenuProvider : IMenuProvider
    {
        private readonly BlockEnhancementsOptions _blockEnhancementsOptions;

        public BlockEnhancementsMenuProvider(BlockEnhancementsOptions blockEnhancementsOptions)
        {
            _blockEnhancementsOptions = blockEnhancementsOptions;
        }

        public IEnumerable<MenuItem> GetMenuItems()
        {
            if (!_blockEnhancementsOptions.LocalBlockConverterEnabled)
            {
                return Enumerable.Empty<MenuItem>();
            }

            var pluginUrl = Paths.ToResource("episerver-labs-block-enhancements" , "LocalContentAnalyzerPlugin");
            var controllerPath = $"{pluginUrl}/Index";

            return new List<MenuItem>
            {
                new UrlMenuItem("Local content analyzer", MenuPaths.Global + "/cms/admin/localblocksconverterplugin",
                    controllerPath)
                {
                    Alignment = MenuItemAlignment.Left,
                    SortIndex = SortIndex.Last,
                    AuthorizationPolicy = CmsPolicyNames.CmsAdmin
                }
            };
        }
    }
}
