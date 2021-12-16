using EPiServer.Shell;
using Microsoft.AspNetCore.Mvc;
// using PlugInArea = EPiServer.PlugIn.PlugInArea;

namespace EPiServer.Labs.BlockEnhancements
{
    //TODO [GuiPlugIn(Area = PlugInArea.AdminMenu, UrlFromModuleFolder = "LocalContentAnalyzerPlugin", DisplayName = "Local content analyzer")]
    public class LocalContentAnalyzerPluginController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            var moduleUrl = Paths.ToResource("episerver-labs-block-enhancements", string.Empty);

            return View("Index", new LocalContentAnalyzerViewModel
            {
                ModuleUrl = moduleUrl
            });
        }
    }
}
