using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace MS_Entra_ID_Blazor_Client.Pages.WeatherPages
{
    public class WeatherBase: ComponentBase
    {
        [Inject]
        MicrosoftIdentityConsentAndConditionalAccessHandler ConsentHandler { get; set; }
        [Inject]
        IDownstreamApi _downstreamApi { get; set; }
        protected IEnumerable<WeatherModel> weatherList = new List<WeatherModel>();

        protected WeatherModel toDo = new WeatherModel();

        protected override async Task OnInitializedAsync()
        {
            await GetToDoListService();
        }

        /// <summary>
        /// Gets all todo list items.
        /// </summary>
        /// <returns></returns>
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        private async Task GetToDoListService()
        {
            try
            {
                weatherList = await _downstreamApi.GetForUserAsync<IEnumerable<WeatherModel>>("WeatherList");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                // Process the exception from a user challenge
                ConsentHandler.HandleException(ex);
            }
        }
    }
}
