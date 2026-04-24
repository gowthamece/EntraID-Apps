using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SharePointCSOM_POC.Controllers
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