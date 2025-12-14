using Entra_ID_MVC_App.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace Entra_ID_MVC_App.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;;
        }

        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        public async Task<IActionResult> Index()
        {
var user = await _graphServiceClient.Me.Request().GetAsync();
ViewData["GraphApiResult"] = user.DisplayName;
            return View();
        }

        /// <summary>
        /// Groups action to display all groups in the organization
        /// Requires Group.Read.All scope for Microsoft Graph API
        /// </summary>
        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        public async Task<IActionResult> Groups()
        {
            var viewModel = new GroupsViewModel();
            
            try
            {
                _logger.LogInformation("Attempting to retrieve groups from Microsoft Graph");

                // Retrieve all groups with proper error handling and paging support
                var groupsCollectionPage = await _graphServiceClient.Groups
                    .Request()
                    .Select("id,displayName,description,groupTypes,mail,visibility")
                    .OrderBy("displayName")
                    .GetAsync();

                var allGroups = new List<Group>();
                
                // Handle pagination to get all groups
                while (groupsCollectionPage != null)
                {
                    allGroups.AddRange(groupsCollectionPage.CurrentPage);
                    
                    // Check if there are more pages
                    if (groupsCollectionPage.NextPageRequest != null)
                    {
                        groupsCollectionPage = await groupsCollectionPage.NextPageRequest.GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                viewModel.Groups = allGroups;
                
                _logger.LogInformation($"Successfully retrieved {allGroups.Count} groups");
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Error retrieving groups from Microsoft Graph: {ErrorCode} - {ErrorMessage}", ex.Error.Code, ex.Error.Message);
                viewModel.ErrorMessage = $"Unable to retrieve groups. Error: {ex.Error.Message}";
                
                // Handle specific error scenarios
                if (ex.Error.Code == "Forbidden" || ex.Error.Code == "InsufficientPermissions")
                {
                    viewModel.ErrorMessage = "Insufficient permissions to read groups. Please ensure Group.Read.All scope is granted.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving groups");
                viewModel.ErrorMessage = "An unexpected error occurred while retrieving groups.";
            }

            return View(viewModel);
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
