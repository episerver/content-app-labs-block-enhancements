using EPiServer.Shell;
using Microsoft.AspNetCore.Mvc;

namespace EPiServer.Labs.BlockEnhancements
{
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
