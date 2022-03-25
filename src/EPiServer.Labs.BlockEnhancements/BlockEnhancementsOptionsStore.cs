using EPiServer.Shell.Services.Rest;
using Microsoft.AspNetCore.Mvc;

namespace EPiServer.Labs.BlockEnhancements
{
    [RestStore("episerverlabsblockenhancementsoptions")]
    public class BlockEnhancementsOptionsStore : RestControllerBase
    {
        private readonly BlockEnhancementsOptions _blockEnhancementsOptions;

        public BlockEnhancementsOptionsStore(BlockEnhancementsOptions blockEnhancementsOptions)
        {
            _blockEnhancementsOptions = blockEnhancementsOptions;
        }

        public ActionResult Get()
        {
            return Rest(_blockEnhancementsOptions);
        }
    }
}
