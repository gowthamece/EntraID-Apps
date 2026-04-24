using System.Web.Mvc;

namespace EntraID.SharePointUpload.Mvc48.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Error(string message)
        {
            ViewBag.ErrorMessage = message;
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
