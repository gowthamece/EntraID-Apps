using Azure.Core;
using Azure.Identity;

namespace EntraID_Blazor_APIM_Client.Services;

public class AuthService
{
    private TokenCredential _credential;
    private readonly IConfiguration _configuration;
    private string _authMethod;
    private readonly string _tenantId;
    private readonly string _clientId = "YOUR_BLAZOR_CLIENT_APP_ID_HERE"; // Blazor Client App ID
    private readonly string _apiClientId = "YOUR_BACKEND_API_APP_ID_HERE"; // Backend API Client ID

    public AuthService(TokenCredential initialCredential, IConfiguration configuration, string initialAuthMethod)
    {
        _credential = initialCredential;
        _configuration = configuration;
        _authMethod = initialAuthMethod;
        _tenantId = "YOUR_TENANT_ID_HERE"; // Your tenant ID
    }

    public TokenCredential GetCredential() => _credential;
    
    public string GetAuthMethod() => _authMethod;

    public async Task<(bool success, string message)> TriggerInteractiveLoginAsync()
    {
        try
        {
            // Create an InteractiveBrowserCredential for interactive authentication
            var interactiveCredential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = _tenantId,
                ClientId = _clientId,
                RedirectUri = new Uri("http://localhost"), // Standard loopback for desktop apps
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            });

            // Test the credential by getting a token
            var apiScope = _configuration["BackendAPI:Scopes"] ?? $"api://{_apiClientId}/.default";
            var tokenRequestContext = new TokenRequestContext(new[] { apiScope });
            
            var token = await interactiveCredential.GetTokenAsync(tokenRequestContext, CancellationToken.None);

            // If successful, update the credential
            _credential = interactiveCredential;
            _authMethod = "Interactive Browser Authentication";

            return (true, "✓ Authentication successful! You can now access the API.");
        }
        catch (Azure.Identity.AuthenticationFailedException authEx)
        {
            var errorMsg = $"Authentication failed: {authEx.Message}";
            Console.WriteLine(errorMsg);
            return (false, errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error during authentication: {ex.Message}";
            Console.WriteLine(errorMsg);
            return (false, errorMsg);
        }
    }

    public async Task<(bool success, string message)> TriggerAzureCliLoginAsync()
    {
        try
        {
            // Launch az login command
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "az",
                Arguments = $"login --scope api://{_apiClientId}/.default",
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null)
            {
                return (false, "Failed to start az login process");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                // Recreate Azure CLI credential
                _credential = new AzureCliCredential();
                _authMethod = "Azure CLI (Refreshed)";
                
                // Test the credential
                var apiScope = _configuration["BackendAPI:Scopes"] ?? $"api://{_apiClientId}/.default";
                var tokenRequestContext = new TokenRequestContext(new[] { apiScope });
                await _credential.GetTokenAsync(tokenRequestContext, CancellationToken.None);

                return (true, "✓ Azure CLI authentication refreshed successfully!");
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                return (false, $"az login failed: {error}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error launching az login: {ex.Message}");
        }
    }

    public async Task<bool> ValidateCredentialAsync()
    {
        try
        {
            var apiScope = _configuration["BackendAPI:Scopes"] ?? $"api://{_apiClientId}/.default";
            var tokenRequestContext = new TokenRequestContext(new[] { apiScope });
            await _credential.GetTokenAsync(tokenRequestContext, CancellationToken.None);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
