using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using Xunit;

namespace EntraID_APIM_IntegrationTests;

/// <summary>
/// Shared test fixture for APIM integration tests.
/// Provides configured HttpClient with DefaultAzureCredential authentication.
/// </summary>
public class ApimTestFixture : IAsyncLifetime
{
    private readonly IConfiguration _configuration;
    private readonly TokenCredential _credential;
    
    public HttpClient HttpClient { get; private set; } = null!;
    public string BaseUrl { get; }
    public string Scope { get; }
    public string TenantId { get; }
    
    public ApimTestFixture()
    {
        // Load configuration from appsettings.json
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
        
        BaseUrl = _configuration["ApimSettings:BaseUrl"] 
            ?? throw new InvalidOperationException("ApimSettings:BaseUrl is not configured");
        Scope = _configuration["ApimSettings:Scope"] 
            ?? throw new InvalidOperationException("ApimSettings:Scope is not configured");
        TenantId = _configuration["ApimSettings:TenantId"] 
            ?? throw new InvalidOperationException("ApimSettings:TenantId is not configured");
        
        // Configure DefaultAzureCredential for Visual Studio and Azure CLI
        // This matches the pattern used in the Blazor client application
        _credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = TenantId,
            ExcludeEnvironmentCredential = true,
            ExcludeWorkloadIdentityCredential = true,
            ExcludeManagedIdentityCredential = true,
            ExcludeVisualStudioCredential = false,      // ✓ Primary: Visual Studio signed-in account
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzureCliCredential = false,          // ✓ Fallback: Azure CLI
            ExcludeAzurePowerShellCredential = true,
            ExcludeInteractiveBrowserCredential = true
        });
    }
    
    public async Task InitializeAsync()
    {
        // Create HttpClient with base address
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
        
        // Acquire token and set Authorization header
        var token = await GetAccessTokenAsync();
        HttpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
    
    public Task DisposeAsync()
    {
        HttpClient?.Dispose();
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Acquires an access token for the configured API scope using DefaultAzureCredential.
    /// Uses Visual Studio's signed-in Azure account or Azure CLI as fallback.
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        var tokenRequestContext = new TokenRequestContext(new[] { Scope });
        var accessToken = await _credential.GetTokenAsync(tokenRequestContext, CancellationToken.None);
        return accessToken.Token;
    }
    
    /// <summary>
    /// Refreshes the Authorization header with a new token.
    /// Use this if tests are long-running and token might expire.
    /// </summary>
    public async Task RefreshTokenAsync()
    {
        var token = await GetAccessTokenAsync();
        HttpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
    
    /// <summary>
    /// Gets the endpoint path for a named endpoint from configuration.
    /// </summary>
    public string GetEndpoint(string endpointName)
    {
        return _configuration[$"Endpoints:{endpointName}"] 
            ?? throw new InvalidOperationException($"Endpoint '{endpointName}' is not configured");
    }
}
