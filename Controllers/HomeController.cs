using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TechnoNimbus_Entra.Models;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace TechnoNimbus_Entra.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GraphHelper _graphHelper;


        public HomeController(IHttpContextAccessor httpContextAccessor,
      IConfiguration configuration)
        {
            string[] graphScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
            _httpContextAccessor = httpContextAccessor;

            if (this._httpContextAccessor.HttpContext != null)
            {
                this._graphHelper = new GraphHelper(this._httpContextAccessor.HttpContext, graphScopes);
            }
        }

        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        public async Task<IActionResult> Index()
        {
            //var user = await _graphServiceClient.Me.Request().GetAsync();
            ViewData["User"] = _httpContextAccessor.HttpContext.User;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
