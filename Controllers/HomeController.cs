using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TechnoNimbus_Entra.Models;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace TechnoNimbus_Entra.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GraphHelper _graphHelper;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;

        public HomeController(IHttpContextAccessor httpContextAccessor,
      IConfiguration configuration ,
      MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler)
        {
            string[] graphScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
            _httpContextAccessor = httpContextAccessor;

            if (this._httpContextAccessor.HttpContext != null)
            {
                this._graphHelper = new GraphHelper(this._httpContextAccessor.HttpContext, graphScopes);
            }

            _consentHandler = consentHandler;
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
            string claimsChallange=CheckForRequiredAuthContext("C1");
            if(!string.IsNullOrEmpty(claimsChallange))
            {
                _consentHandler.ChallengeUser(new string[] { "user.read" }, claimsChallange);
                return new EmptyResult();
            }
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public string CheckForRequiredAuthContext(string authContextId)
        {
            string claimChallange = string.Empty;
            string saveAutheContextId = "C1";
            HttpContext context = this.HttpContext;
            string authContextClassReferenceClaim = "acrs";
            if(context ==null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            Claim acrsClaim = context.User.FindAll(authContextClassReferenceClaim).FirstOrDefault(x=>x.Value==saveAutheContextId);
           if(acrsClaim?.Value == saveAutheContextId)
            {
                claimChallange = "{\"id_token\":{\"acrs\":{\"essential\":true;\"value\"\"" + saveAutheContextId + "\"}}}";
            }
            return claimChallange;

        }
    }
}
