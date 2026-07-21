# EntraID-FunctionApp-Client

Blazor Web App client that calls the protected Function endpoint:

- Endpoint: https://func-entraid-dev-eus01-fmc0gpdbb9dwgvc9.eastus-01.azurewebsites.net/api/auth/ping
- Auth flow: Microsoft Entra token from managed identity (App Service) or developer identity (local)

## Configuration

`appsettings.json`

- `AzureAd:UseManagedIdentity`: `true` for App Service system-assigned managed identity
- `AzureAd:ManagedIdentityClientId`: keep empty for system-assigned identity
- `FunctionApi:BaseUrl`: Function host URL
- `FunctionApi:PingPath`: Function route path
- `FunctionApi:Scope`: use `api://<FunctionApiAppId>/.default` for app-only token requests

## Run locally

```powershell
cd EntraID-FunctionApp-Client
dotnet run
```

Open `/function-ping` and invoke the endpoint.

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

Managed identity uses app-only tokens and typically carries `roles` claims, not delegated `scp` claims. The Function API should validate either the delegated scope or the required app role for this client scenario.
