# EntraID-FunctionApp-Client

Blazor Web App client that signs users in with Entra ID and calls the protected Function endpoint using managed identity:

- Endpoint: https://func-entraid-dev-eus01-fmc0gpdbb9dwgvc9.eastus-01.azurewebsites.net/api/auth/ping
- Auth flow: Entra ID sign-in for the user, then `DefaultAzureCredential` for the Function API call

## Configuration

`appsettings.json`

- `AzureAd:UseManagedIdentity`: `true` for App Service system-assigned managed identity
- `AzureAd:ManagedIdentityClientId`: keep empty for system-assigned identity
- `FunctionApi:BaseUrl`: Function host URL
- `FunctionApi:PingPath`: Function route path
- `FunctionApi:Scope`: use `api://<FunctionApiAppId>/.default` for managed identity / app-only token requests

## Run locally

```powershell
cd EntraID-FunctionApp-Client
dotnet run
```

Open `/function-ping` after signing in and invoke the endpoint.

## Deploy to App Service with system-assigned managed identity

1. Publish this app to Azure App Service.
2. Enable system-assigned identity on the App Service.
3. Assign API permission to that managed identity using:

```powershell
./Scripts/Grant-FunctionApiPermissionToManagedIdentity.ps1 `
  -ResourceGroupName "<resource-group>" `
  -WebAppName "<blazor-webapp-name>" `
  -FunctionApiAppId "<function-api-app-registration-client-id>" `
  -ApiAppRoleValue "access_as_application"
```

## Important auth note

The page is protected by Entra ID sign-in, but the outbound Function API call uses `DefaultAzureCredential`.
In Azure App Service, that resolves to the app's managed identity.
The Function API should accept an application permission/app role for this caller path.
