using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;

namespace EntraID_Blazor_APIM_Client.Services;

public class ApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenCredential _credential;
    private readonly IConfiguration _configuration;

    public ApiService(
        IHttpClientFactory httpClientFactory,
        TokenCredential credential,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _credential = credential;
        _configuration = configuration;
    }

    public async Task<string> GetWeatherForecastAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("BackendAPI");
            
            // Get token using the injected credential
            // The scope should be: api://{YOUR_API_CLIENT_ID}/.default
            var apiScope = _configuration["BackendAPI:Scopes"] ?? "api://YOUR_API_CLIENT_ID/.default";
            var tokenRequestContext = new TokenRequestContext(new[] { apiScope });
            
            // Use the credential registered in Program.cs
            var accessToken = await _credential.GetTokenAsync(tokenRequestContext, CancellationToken.None);

            // Debug: Decode and log token claims
            var tokenParts = accessToken.Token.Split('.');
            if (tokenParts.Length >= 2)
            {
                var payload = tokenParts[1];
                var paddedPayload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var decodedBytes = Convert.FromBase64String(paddedPayload);
                var decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
                Console.WriteLine($"Token Claims: {decodedJson}");
            }

            // Add the access token to the request
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken.Token);

            // Call the API (updated for APIM mock endpoint)
            var response = await httpClient.GetAsync("/weather/forecast");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }
}
