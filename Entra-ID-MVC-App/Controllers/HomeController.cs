using Entra_ID_MVC_App.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.IdentityModel.Tokens.Jwt;

namespace Entra_ID_MVC_App.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
      //  private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
          //  _graphServiceClient = graphServiceClient;;
        }

     //   [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        public async Task<IActionResult> Index()
        {
//var user = await _graphServiceClient.Me.Request().GetAsync();
            var cookieResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                .ConfigureAwait(false);

            ViewData["GraphApiResult"] = "No Graph Client";
            ViewData["IdTokenRemainingTime"] = await GetTokenRemainingTimeAsync("id_token");
            ViewData["SessionCookieRemainingTime"] = GetSessionCookieRemainingTime(cookieResult);
            ViewData["AuthTime"] = GetAuthTime();
            ViewData["SignInType"] = GetSignInType(cookieResult);
            ViewData["AllClaims"] = GetAllClaims();
            ViewData["PostMessage"] = TempData["PostMessage"];
            ViewData["SessionExpiredMessage"] = TempData["SessionExpiredMessage"];
            ViewData["ClearDraft"] = TempData["ClearDraft"];
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForm(string message)
        {
            var cookieResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                .ConfigureAwait(false);

            if (!cookieResult.Succeeded || cookieResult.Principal?.Identity?.IsAuthenticated != true)
            {
                TempData["SessionExpiredMessage"] = "Your session has expired. Please sign in again. Your form data has been preserved in the browser and will be restored after you authenticate.";
                return Challenge(new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(Index), "Home")
                }, OpenIdConnectDefaults.AuthenticationScheme);
            }

            TempData["PostMessage"] = string.IsNullOrWhiteSpace(message)
                ? "Form submitted, but no message was entered."
                : $"Form submitted successfully at {DateTime.Now:HH:mm:ss}. Message: {message}";
            TempData["ClearDraft"] = "true";

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult SessionStatus()
        {
            return User?.Identity?.IsAuthenticated == true ? Ok() : Unauthorized();
        }

        private async Task<string> GetTokenRemainingTimeAsync(string tokenType)
        {
            var token = await HttpContext.GetTokenAsync(tokenType).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
            {
                return "Not available";
            }

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return "Not available";
            }

            var jwtToken = handler.ReadJwtToken(token);
            if (jwtToken.ValidTo == DateTime.MinValue)
            {
                return "Not available";
            }

            return FormatRemainingTime(jwtToken.ValidTo);
        }

        private async Task<string> GetTokenExpiryTimeAsync(string tokenType)
        {
            var token = await HttpContext.GetTokenAsync(tokenType).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
            {
                return "Not available";
            }

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return "Not available";
            }

            var jwtToken = handler.ReadJwtToken(token);
            if (jwtToken.ValidTo == DateTime.MinValue)
            {
                return "Not available";
            }

            return jwtToken.ValidTo.ToLocalTime().ToString("u");
        }

        private async Task<string> GetAuthTimeAsync()
        {
            var token = await HttpContext.GetTokenAsync("id_token").ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
            {
                return "Not available";
            }

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return "Not available";
            }

            var jwtToken = handler.ReadJwtToken(token);
            var authTimeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "auth_time")?.Value;
            if (string.IsNullOrWhiteSpace(authTimeClaim) || !long.TryParse(authTimeClaim, out var seconds))
            {
                return "Not available";
            }

            return DateTimeOffset.FromUnixTimeSeconds(seconds).ToLocalTime().ToString("u");
        }

        private static string GetSessionCookieRemainingTime(AuthenticateResult cookieResult)
        {
            var expiresUtc = cookieResult.Properties?.ExpiresUtc;
            return expiresUtc.HasValue
                ? FormatRemainingTime(expiresUtc.Value.UtcDateTime)
                : "Not available";
        }

        private string GetAuthTime()
        {
            var authTimeClaim = User.FindFirst("auth_time")?.Value;
            if (string.IsNullOrWhiteSpace(authTimeClaim) || !long.TryParse(authTimeClaim, out var unixSeconds))
            {
                return "Not available";
            }

            var authTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return authTime.ToLocalTime().ToString("u");
        }

        private string GetSignInType(AuthenticateResult cookieResult)
        {
            var authTimeClaim = User.FindFirst("auth_time")?.Value;
            if (string.IsNullOrWhiteSpace(authTimeClaim) || !long.TryParse(authTimeClaim, out var unixSeconds))
            {
                return "Not available";
            }

            if (!cookieResult.Properties?.IssuedUtc.HasValue ?? true)
            {
                return "Not available";
            }

            var authTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            var issuedUtc = cookieResult.Properties.IssuedUtc.Value.UtcDateTime;
            var difference = issuedUtc - authTime;

            return difference > TimeSpan.FromMinutes(5)
                ? "Silent / SSO (inferred)"
                : "Interactive (inferred)";
        }

        private IReadOnlyList<string> GetAllClaims()
        {
            return User.Claims
                .OrderBy(claim => claim.Type, StringComparer.OrdinalIgnoreCase)
                .ThenBy(claim => claim.Value, StringComparer.OrdinalIgnoreCase)
                .Select(claim => $"{claim.Type}: {claim.Value}")
                .ToList();
        }

        private static string FormatRemainingTime(DateTime expiryUtc)
        {
            var remaining = expiryUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return "Expired";
            }

            return $"{remaining.Days}d {remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s";
        }

        /// <summary>
        /// Groups action to display all groups in the organization
        /// Requires Group.Read.All scope for Microsoft Graph API
        /// </summary>
     //   [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        //public async Task<IActionResult> Groups()
        //{
        //    var viewModel = new GroupsViewModel();
            
        //    try
        //    {
        //        _logger.LogInformation("Attempting to retrieve groups from Microsoft Graph");

        //        // Retrieve all groups with proper error handling and paging support
        //        var groupsCollectionPage = await _graphServiceClient.Groups
        //            .Request()
        //            .Select("id,displayName,description,groupTypes,mail,visibility")
        //            .OrderBy("displayName")
        //            .GetAsync();

        //        var allGroups = new List<Group>();
                
        //        // Handle pagination to get all groups
        //        while (groupsCollectionPage != null)
        //        {
        //            allGroups.AddRange(groupsCollectionPage.CurrentPage);
                    
        //            // Check if there are more pages
        //            if (groupsCollectionPage.NextPageRequest != null)
        //            {
        //                groupsCollectionPage = await groupsCollectionPage.NextPageRequest.GetAsync();
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }

        //        viewModel.Groups = allGroups;
                
        //        _logger.LogInformation($"Successfully retrieved {allGroups.Count} groups");
        //    }
        //    catch (ServiceException ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving groups from Microsoft Graph: {ErrorCode} - {ErrorMessage}", ex.Error.Code, ex.Error.Message);
        //        viewModel.ErrorMessage = $"Unable to retrieve groups. Error: {ex.Error.Message}";
                
        //        // Handle specific error scenarios
        //        if (ex.Error.Code == "Forbidden" || ex.Error.Code == "InsufficientPermissions")
        //        {
        //            viewModel.ErrorMessage = "Insufficient permissions to read groups. Please ensure Group.Read.All scope is granted.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Unexpected error retrieving groups");
        //        viewModel.ErrorMessage = "An unexpected error occurred while retrieving groups.";
        //    }

        //    return View(viewModel);
        //}

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
