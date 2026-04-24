using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;

namespace SharePointCSOM_POC.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public void SignIn(string returnUrl = "/")
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = returnUrl },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
                return;
            }

            Response.Redirect(returnUrl);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignOut()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(
                CookieAuthenticationDefaults.AuthenticationType,
                OpenIdConnectAuthenticationDefaults.AuthenticationType);

            return RedirectToAction("Index", "Home");
        }
    }
}