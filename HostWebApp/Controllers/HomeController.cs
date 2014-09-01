using System.Web;
using System.Web.Mvc;
using MvcLib.FsDump;

namespace HostWebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewData["Message"] = "Your application description page.....";

            return View();
        }

        public ActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public ActionResult Reset()
        {            
            HttpRuntime.UnloadAppDomain();
            return RedirectToAction("Index", new { q = "Reset success" });
        }

        public ActionResult Refresh()
        {
            DbToLocal.Execute();
            
            return RedirectToAction("Index", new { q = "Refresh success" });
        }
    }
}




