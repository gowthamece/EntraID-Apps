# Blazor .NET 8 with Entra ID, Azure APIM & Managed Identity

Complete end-to-end solution demonstrating secure authentication and authorization using Microsoft Entra ID, Azure API Management, and Managed Identity.

## 🎯 Solution Overview

This solution includes:

1. **Blazor .NET 8 Client App** - Interactive server-side app with Managed Identity authentication
2. **Backend Web API** - Protected API with JWT validation and App Roles
3. **Azure APIM** - API Gateway with JWT validation policy
4. **Azure VM Deployment** - Hosting the Blazor app with User-Assigned Managed Identity
5. **Complete Auth Flow** - OAuth 2.0 Client Credentials with Managed Identity

## 🔐 Security Highlights

- **No credentials in code** - Managed Identity eliminates secrets from configuration
- **App Role-based access** - Only authorized identities can call the API
- **JWT validation at APIM** - Token verification before reaching backend
- **Audit trail** - All access logged in Azure AD and APIM analytics

## 🏗️ Architecture

```
┌─────────────────┐
│   User Browser  │
└────────┬────────┘
         │ 1. Navigate to App
         ↓
┌─────────────────────────────────────┐
│  Azure VM (IIS)                     │
│  Blazor Client Application          │
│  User-Assigned Managed Identity     │
│  (mi-blazor-apim-client)            │
│  Client ID: YOUR_MI_CLIENT_ID       │
└────────┬────────────────────────────┘
         │ 2. Request Token (No credentials)
         ↓
┌─────────────────────────────────────┐
│  Microsoft Entra ID                 │
│  Token: api://YOUR_API_ID/.default  │
│  App Role: weather.read             │
└────────┬────────────────────────────┘
         │ 3. JWT Access Token
         ↓
┌─────────────────────────────────────┐
│  Azure API Management               │
│  validate-jwt Policy                │
│  - Verify issuer & audience         │
│  - Check required roles             │
└────────┬────────────────────────────┘
         │ 4. Validated Request
         ↓
┌─────────────────────────────────────┐
│  Backend API                        │
│  /weather/forecast                  │
└─────────────────────────────────────┘
```

## 📋 Prerequisites

- .NET 8 SDK
- Azure CLI
- Azure subscription
- Visual Studio Code or Visual Studio 2022
- PowerShell 7+

## 🚀 Quick Start

### 1. Clone and Setup

```powershell
cd EntraID-Apps
```

### 2. Create Entra ID App Registrations

Follow detailed steps in [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md#step-1-create-entra-id-app-registrations)

**Quick Summary:**
- Create Backend API app registration (expose API scope)
- Create Blazor Client app registration (grant API permissions)
- Copy Client IDs, Tenant ID, and Client Secret

### 3. Update Configuration Files

**Backend API:** `EntraID-APIM-BackendAPI/appsettings.json`
```json
{
  "AzureAd": {
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_API_CLIENT_ID"
  }
}
```

**Blazor Client:** `EntraID-Blazor-APIM-Client/appsettings.json`
```json
{
  "AzureAd": {
    "TenantId": "YOUR_TENANT_ID_HERE",
    "ClientId": "YOUR_MANAGED_IDENTITY_CLIENT_ID_HERE",
    "UseManagedIdentity": true,
    "UseClientCredentials": false
  },
  "BackendAPI": {
    "BaseUrl": "https://your-apim-instance.azure-api.net",
    "Scopes": "api://YOUR_BACKEND_API_APP_ID_HERE/.default"
  }
}
```

## 🔑 Managed Identity Setup

### 1. Create User-Assigned Managed Identity

```powershell
# In Azure Portal: Managed Identities → Create
# Name: mi-blazor-apim-client
# Note the Client ID: 29fb906c-820b-4440-acd1-6baab44bfd42
```

### 2. Assign App Role to Managed Identity

```powershell
Connect-AzAccount

$managedIdentityClientId = "YOUR_MANAGED_IDENTITY_CLIENT_ID_HERE"
$backendApiAppId = "YOUR_BACKEND_API_APP_ID_HERE"

$miSP = Get-AzADServicePrincipal -Filter "appId eq '$managedIdentityClientId'"
$apiSP = Get-AzADServicePrincipal -Filter "appId eq '$backendApiAppId'"

# Assign the weather.read role
New-AzADServicePrincipalAppRoleAssignment `
    -ServicePrincipalId $miSP.Id `
    -ResourceId $apiSP.Id `
    -AppRoleId "YOUR_APP_ROLE_ID_HERE"
```

### 3. Assign Identity to VM

```
Azure Portal → Virtual Machine → Identity → User assigned → Add
Select: mi-blazor-apim-client
```

### 4. Configure APIM JWT Validation Policy

```xml
<inbound>
    <validate-jwt header-name="Authorization" failed-validation-httpcode="401">
        <openid-config url="https://login.microsoftonline.com/YOUR_TENANT_ID_HERE/v2.0/.well-known/openid-configuration" />
        <audiences>
            <audience>api://YOUR_BACKEND_API_APP_ID_HERE</audience>
        </audiences>
        <required-claims>
            <claim name="roles" match="any">
                <value>weather.read</value>
            </claim>
        </required-claims>
    </validate-jwt>
</inbound>
```

### 4. Test Locally

**Terminal 1 - Backend API:**
```powershell
cd EntraID-APIM-BackendAPI
dotnet run --urls "https://localhost:5001"
```

**Terminal 2 - Blazor Client:**
```powershell
cd EntraID-Blazor-APIM-Client
dotnet run --urls "https://localhost:7001"
```

Open browser: `https://localhost:7001/weather`

### 5. Deploy to Azure (Automated)

Run the PowerShell deployment script:

```powershell
# Set your parameters
$securePassword = ConvertTo-SecureString "YourVMPassword123!" -AsPlainText -Force

.\Deploy-AzureResources.ps1 `
    -ResourceGroupName "rg-blazor-apim-demo" `
    -Location "eastus" `
    -TenantId "YOUR_TENANT_ID" `
    -ApiClientId "YOUR_API_CLIENT_ID" `
    -BlazorClientId "YOUR_BLAZOR_CLIENT_ID" `
    -AdminPassword $securePassword
```

This script creates:
- ✅ Azure APIM service
- ✅ App Service for Backend API
- ✅ Azure VM for Blazor app
- ✅ Key Vault for secrets
- ✅ Application Insights for monitoring
- ✅ Managed Identities on all resources

## 📁 Project Structure

```
EntraID-Apps/
├── EntraID-Blazor-APIM-Client/        # Blazor client application
│   ├── Components/
│   │   └── Pages/
│   │       └── Weather.razor          # Demo page calling API
│   ├── Services/
│   │   └── ApiService.cs              # API client with token acquisition
│   ├── Program.cs                     # App configuration with auth
│   └── appsettings.json               # Configuration
│
├── EntraID-APIM-BackendAPI/           # Backend Web API
│   ├── Program.cs                     # API with JWT validation
│   └── appsettings.json               # Configuration
│
├── DEPLOYMENT_GUIDE.md                # Complete deployment guide
├── APIM_POLICIES.md                   # APIM policy configurations
├── Deploy-AzureResources.ps1          # Automated deployment script
└── README.md                          # This file
```

## 🔐 Authentication Flow

1. **User accesses Blazor app** → Application loads on Azure VM
2. **App needs API data** → Requests token using Managed Identity
3. **Entra ID validates MI** → Issues JWT with `weather.read` role
4. **Token included in request** → `Authorization: Bearer {token}`
5. **APIM validates JWT** → Checks audience, issuer, and roles
6. **APIM forwards to backend** → Only if token is valid
7. **API returns data** → Displayed in Blazor app

## 🛡️ Why Managed Identity?

| Traditional Approach | Managed Identity Approach |
|---------------------|---------------------------|
| Store secrets in config | No secrets needed |
| Rotate credentials manually | Azure handles rotation |
| Risk of credential exposure | Zero credential exposure |
| Complex token refresh logic | Automatic token management |
| Hard to audit | Full audit trail in Azure AD |

## 🔧 Configuration Details

### Blazor Client Features

- ✅ OpenID Connect authentication
- ✅ Token acquisition for downstream APIs
- ✅ In-memory token caching
- ✅ Automatic token refresh
- ✅ Secure HttpClient with bearer tokens

### Backend API Features

- ✅ JWT bearer authentication
- ✅ Audience validation
- ✅ Scope-based authorization
- ✅ Swagger/OpenAPI support

### APIM Features

- ✅ JWT token validation
- ✅ Rate limiting
- ✅ Request/response transformation
- ✅ Managed Identity for backend auth
- ✅ CORS support
- ✅ Comprehensive logging

## 📊 Testing

### Test JWT Validation

```powershell
# Get token from browser DevTools after login
$token = "YOUR_ACCESS_TOKEN"

# Call APIM endpoint
curl -X GET "https://apim-pi-tracking.azure-api.net/api/weatherforecast" `
  -H "Authorization: Bearer $token"
```

### Decode JWT Token

Visit [jwt.ms](https://jwt.ms) and paste your token to inspect claims:
- `aud`: Should match `api://YOUR_API_CLIENT_ID`
- `iss`: Should be Microsoft Entra ID
- `scp`: Should contain `access_as_user`

## 🛠️ Troubleshooting

### Common Issues

**401 Unauthorized**
- ✅ Check token audience matches API Client ID
- ✅ Verify APIM JWT validation policy
- ✅ Confirm backend API configuration

**AADSTS50011: Redirect URI mismatch**
- ✅ Add correct redirect URI in Entra ID app registration
- ✅ Format: `https://your-domain/signin-oidc`

**Unable to acquire token**
- ✅ Grant admin consent for API permissions
- ✅ Check scopes match exposed API
- ✅ Verify client secret is valid

**APIM 403 Forbidden**
- ✅ Check APIM subscription key if required
- ✅ Verify JWT validation policy configuration

See [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md#step-10-troubleshooting-guide) for detailed troubleshooting.

## 📚 Documentation

- **[DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)** - Complete step-by-step deployment guide
- **[APIM_POLICIES.md](./APIM_POLICIES.md)** - APIM policy configurations and examples
- **[Deploy-AzureResources.ps1](./Deploy-AzureResources.ps1)** - Automated deployment script

## 🔐 Security Best Practices

1. ✅ **Never commit secrets** - Use Azure Key Vault
2. ✅ **Use Managed Identity** - Eliminate credential management
3. ✅ **Validate tokens** - At both APIM and backend
4. ✅ **Enable HTTPS only** - Force secure connections
5. ✅ **Implement rate limiting** - Prevent abuse
6. ✅ **Short token expiration** - 1 hour recommended
7. ✅ **Monitor and log** - Use Application Insights

## 📦 NuGet Packages Used

**Blazor Client:**
- Microsoft.Identity.Web (3.3.0)
- Microsoft.Identity.Web.UI (3.3.0)

**Backend API:**
- Microsoft.Identity.Web (3.3.0)

## 🌐 Azure Resources Created

| Resource | Type | Purpose |
|----------|------|---------|
| apim-pi-tracking | API Management | API Gateway with JWT validation |
| entraid-backend-api-app | App Service | Backend API hosting |
| blazor-client-vm | Virtual Machine | Blazor app hosting |
| kv-blazor-* | Key Vault | Secure secret storage |
| blazor-app-insights | Application Insights | Monitoring and diagnostics |

## 📝 Key Concepts

### Managed Identity

Managed Identity provides Azure resources with automatically managed identities for authenticating to Azure services without storing credentials.

**Benefits:**
- No credentials in code
- Automatic credential rotation
- Azure RBAC integration
- Reduced security risk

### JWT Validation

JSON Web Tokens (JWT) are validated at multiple layers:
1. **APIM Layer** - Fast validation before reaching backend
2. **Backend Layer** - Additional validation for defense in depth

**Validation Checks:**
- Token signature
- Expiration time
- Issuer
- Audience
- Required claims

## 🎓 Learning Resources

- [Microsoft Identity Platform](https://learn.microsoft.com/en-us/entra/identity-platform/)
- [Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/)
- [Managed Identities](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/)
- [Blazor Authentication](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)

## 🤝 Contributing

This is a demonstration project. Feel free to:
- Extend functionality
- Improve documentation
- Add more examples
- Report issues

## 📄 License

This project is provided as-is for educational purposes.

## 👤 Author

Created as part of the EntraID-Apps project collection.

---

**Last Updated:** January 14, 2026

**Version:** 2.0.0 - Added Managed Identity Support
