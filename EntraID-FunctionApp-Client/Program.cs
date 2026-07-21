using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using EntraID_FunctionApp_Client.Components;
using EntraID_FunctionApp_Client.Services;

var builder = WebApplication.CreateBuilder(args);

var functionApiBaseUrl = builder.Configuration["FunctionApi:BaseUrl"]
    ?? "https://func-entraid-dev-eus01-fmc0gpdbb9dwgvc9.eastus-01.azurewebsites.net";

var tenantId = builder.Configuration["AzureAd:TenantId"]
    ?? throw new InvalidOperationException("Missing configuration: AzureAd:TenantId");
var clientId = builder.Configuration["AzureAd:ClientId"]
    ?? throw new InvalidOperationException("Missing configuration: AzureAd:ClientId");
var clientSecret = builder.Configuration["AzureAd:ClientSecret"];
var callbackPath = builder.Configuration["AzureAd:CallbackPath"] ?? "/signin-oidc";
var apiScope = builder.Configuration["FunctionApi:Scope"]
    ?? "api://84a651ee-de65-4753-ba10-f89389c9308d/access_as_user";

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.ClientId = clientId;
        options.CallbackPath = callbackPath;
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = false;

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            options.ClientSecret = clientSecret;
        }

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("offline_access");
        options.Scope.Add(apiScope);
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHttpClient("FunctionApi", client =>
{
    client.BaseAddress = new Uri(functionApiBaseUrl);
});
builder.Services.AddScoped<FunctionApiService>();

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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/login", async (HttpContext context, string? returnUrl) =>
{
    var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;

    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect(redirectUri);
        return;
    }

    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = redirectUri
    });
});

app.MapGet("/logout", async (HttpContext context, string? returnUrl) =>
{
    var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = redirectUri
    });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
