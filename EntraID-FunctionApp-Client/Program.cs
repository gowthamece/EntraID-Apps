using Azure.Core;
using Azure.Identity;
using EntraID_FunctionApp_Client.Components;
using EntraID_FunctionApp_Client.Services;

var builder = WebApplication.CreateBuilder(args);

var functionApiBaseUrl = builder.Configuration["FunctionApi:BaseUrl"]
    ?? "https://func-entraid-dev-eus01-fmc0gpdbb9dwgvc9.eastus-01.azurewebsites.net";

var useManagedIdentity = builder.Configuration.GetValue("AzureAd:UseManagedIdentity", true);
var managedIdentityClientId = builder.Configuration["AzureAd:ManagedIdentityClientId"];

TokenCredential credential;
if (useManagedIdentity)
{
    credential = string.IsNullOrWhiteSpace(managedIdentityClientId)
        ? new ManagedIdentityCredential()
        : new ManagedIdentityCredential(managedIdentityClientId);
}
else
{
    credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ManagedIdentityClientId = managedIdentityClientId
    });
}

builder.Services.AddSingleton(credential);
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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
