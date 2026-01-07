using Azure.Core;
using Azure.Identity;
using EntraID_Blazor_APIM_Client.Components;
using EntraID_Blazor_APIM_Client.Services;

var builder = WebApplication.CreateBuilder(args);

TokenCredential credential;
string authMethod;

var tenantId = "YOUR_TENANT_ID_HERE";


try
{
    credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        TenantId = tenantId,
        ExcludeEnvironmentCredential = true,  // Exclude client secret from env vars
        ExcludeWorkloadIdentityCredential = true,
        ExcludeManagedIdentityCredential = true, 
        ExcludeVisualStudioCredential = false,  // Try Visual Studio first
        ExcludeVisualStudioCodeCredential = true,  
        ExcludeAzureCliCredential = false,  // Enable Azure CLI as fallback
        ExcludeAzurePowerShellCredential = true,  
        ExcludeInteractiveBrowserCredential = true 
    });
    
    authMethod = "DefaultAzureCredential (Developer Identity)";
    
}
catch (Exception ex)
{
    Console.WriteLine($"DefaultAzureCredential setup failed: {ex.Message}");
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
