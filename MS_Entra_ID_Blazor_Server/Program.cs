using MS_Entra_ID_Blazor_Server.Components;
using MS_Entra_ID_Blazor_Server.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using GraphServiceClient = Microsoft.Graph.GraphServiceClient;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Detect if running in Azure or locally
var isAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));

if (isAzure)
{
    // Azure: Simple authentication for user login (no Graph token acquisition)
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
}
else
{
    // Local: Authentication with delegated Graph API access
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
        .AddInMemoryTokenCaches();
}

// Add authorization
builder.Services.AddAuthorization();

// Add cascading authentication state
builder.Services.AddCascadingAuthenticationState();

// Add controllers for authentication UI
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Register Graph Service based on environment
if (isAzure)
{
    // Azure: Use Managed Identity - create instance directly
    builder.Services.AddSingleton<ManagedIdentityGraphService>(sp =>
    {
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        return new ManagedIdentityGraphService(env);
    });
}
else
{
    // Local: Use delegated permissions - inject GraphServiceClient
    builder.Services.AddScoped<ManagedIdentityGraphService>(sp =>
    {
        var graphClient = sp.GetRequiredService<GraphServiceClient>();
        return new ManagedIdentityGraphService(graphClient);
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map controllers for authentication
app.MapControllers();

app.Run();
