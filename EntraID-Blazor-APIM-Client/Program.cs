using Azure.Core;
using Azure.Identity;
using EntraID_Blazor_APIM_Client.Components;
using EntraID_Blazor_APIM_Client.Services;

var builder = WebApplication.CreateBuilder(args);

TokenCredential credential;
string authMethod;

var tenantId = builder.Configuration["AzureAd:TenantId"] ?? "YOUR_TENANT_ID_HERE";
var clientId = builder.Configuration["AzureAd:ClientId"] ?? "YOUR_CLIENT_ID_HERE";
var clientSecret = builder.Configuration["AzureAd:ClientSecret"];
var useManagedIdentity = builder.Configuration.GetValue<bool>("AzureAd:UseManagedIdentity");
var useClientCredentials = builder.Configuration.GetValue<bool>("AzureAd:UseClientCredentials");

try
{
    if (useManagedIdentity)
    {
        // Use Managed Identity with specific client ID (User-Assigned Managed Identity)
        // This requires a User-Assigned Managed Identity assigned to the VM
        credential = new ManagedIdentityCredential(clientId);
        authMethod = $"Managed Identity (Client ID: {clientId})";
        Console.WriteLine($"Using Managed Identity with Client ID: {clientId}");
    }
    else if (useClientCredentials && !string.IsNullOrEmpty(clientSecret))
    {
        // Use Client Credentials flow with App Registration
        // This uses the app's client ID and secret to authenticate
        credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        authMethod = $"Client Credentials (App ID: {clientId})";
        Console.WriteLine($"Using Client Credentials with App ID: {clientId}");
    }
    else
    {
        // Fallback to DefaultAzureCredential for local development
        credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = tenantId,
            ManagedIdentityClientId = clientId, // Use specific MI if available
            ExcludeEnvironmentCredential = true,
            ExcludeWorkloadIdentityCredential = true,
            ExcludeManagedIdentityCredential = false, // Enable Managed Identity
            ExcludeVisualStudioCredential = false,
            ExcludeVisualStudioCodeCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeAzurePowerShellCredential = true,
            ExcludeInteractiveBrowserCredential = true
        });
        authMethod = "DefaultAzureCredential (Managed Identity or Developer Identity)";
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Credential setup failed: {ex.Message}");
    throw;
}

builder.Services.AddSingleton<TokenCredential>(credential);
builder.Services.AddSingleton(new AuthenticationInfo { Method = authMethod });

// Register Auth Service for dynamic authentication
builder.Services.AddSingleton(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new AuthService(credential, config, authMethod);
});

// Register HttpClient for calling the API
builder.Services.AddHttpClient("BackendAPI", client =>
{
    var apimEndpoint = builder.Configuration["BackendAPI:BaseUrl"];
    client.BaseAddress = new Uri(apimEndpoint ?? "https://your-apim-gateway.azure-api.net");
});

// Register API Service
builder.Services.AddScoped<ApiService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Set the path base for IIS virtual application deployment
app.UsePathBase("/BlazorApiClient");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Authentication info class for tracking which credential method is being used
public class AuthenticationInfo
{
    public string Method { get; set; } = "Unknown";
}
