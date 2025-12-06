# Blazor Server with Microsoft Entra ID Authentication and Managed Identity

This Blazor Server application demonstrates authentication with Microsoft Entra ID and uses Azure Managed Identity to access Microsoft Graph API without storing client secrets.

## Features

✅ **Microsoft Entra ID Authentication** - Secure login/logout with OpenID Connect  
✅ **Managed Identity Support** - No client secrets required when deployed to Azure  
✅ **Microsoft Graph Integration** - Display all Entra ID groups using Graph API  
✅ **User Profile Page** - Show authenticated user information  
✅ **Groups Page** - Display all groups with Group.Read.All permission  

## Project Structure

```
MS_Entra_ID_Blazor_Server/
├── Pages/
│   ├── Groups.razor           # Displays all Entra ID groups
│   ├── UserProfile.razor      # Shows current user profile
│   └── Authentication.razor   # Authentication page (minimal)
├── Shared/
│   ├── LoginDisplay.razor     # Login/logout UI component
│   └── RedirectToLogin.razor  # Redirect helper
├── Components/
│   └── Layout/
│       └── NavMenu.razor      # Navigation with Groups link
├── Program.cs                 # Authentication & Graph SDK configuration
├── appsettings.json          # Entra ID and Graph configuration
└── AZURE_DEPLOYMENT.md       # Comprehensive deployment guide
```

## Configuration

### appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "YOUR-TENANT-ID",
    "ClientId": "YOUR-CLIENT-ID",
    "CallbackPath": "/signin-oidc"
  },
  "MicrosoftGraph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "Group.Read.All"
  }
}
```

### Required NuGet Packages

- `Microsoft.Identity.Web` (v3.8.1)
- `Microsoft.Identity.Web.MicrosoftGraph` (v3.8.1)
- `Microsoft.Identity.Web.UI` (v3.8.1)
- `Microsoft.AspNetCore.Authentication.OpenIdConnect` (v9.0.3)

## How It Works

### Authentication Flow

1. User clicks "Log in" → Redirects to Microsoft login page
2. User authenticates with Entra ID credentials
3. Application receives authentication token
4. User is redirected back to application

### Managed Identity in Azure

When deployed to Azure Web App with Managed Identity enabled:

1. **No Client Secret Required** - Azure automatically handles authentication
2. **Automatic Token Management** - Tokens are obtained and renewed automatically
3. **Graph API Access** - Managed Identity is granted `Group.Read.All` permission
4. **Secure by Default** - No credentials stored in configuration

### Local Development

For local development, the app uses your Azure credentials from:
- Azure CLI (`az login`)
- Visual Studio Azure Service Authentication
- Azure PowerShell

## Pages Overview

### 1. Home (`/`)
- Landing page with welcome message

### 2. User Profile (`/profile`)
- Displays authenticated user information from Graph API
- Shows: Display Name, UPN, Mail, Job Title, Office Location

### 3. Entra ID Groups (`/groups`)
- Lists all groups the user can access
- Displays: Group Name, Description, Mail, Mail Enabled, Security Enabled, Group ID
- Uses `Group.Read.All` permission

## Deployment to Azure

See **[AZURE_DEPLOYMENT.md](./AZURE_DEPLOYMENT.md)** for comprehensive step-by-step deployment instructions.

### Quick Deployment Steps

1. **Create Azure Web App** (.NET 10 runtime)
2. **Enable Managed Identity** (System-assigned)
3. **Grant Graph Permissions** (Group.Read.All to Managed Identity)
4. **Configure App Settings** (AzureAd and MicrosoftGraph sections)
5. **Update Entra ID App Registration** (Add redirect URI)
6. **Deploy Application** (Visual Studio, CLI, or GitHub Actions)

## Important Security Notes

### ⚠️ Never Commit Secrets
- Do not store client secrets in appsettings.json
- Use Azure Key Vault for sensitive configuration
- Use Managed Identity in Azure environments

### ✅ Best Practices
- Limit Graph API permissions to minimum required
- Enable diagnostic logging for security auditing
- Use HTTPS in production
- Implement proper error handling
- Regular security updates for NuGet packages

## Testing Locally

1. **Login to Azure CLI:**
   ```powershell
   az login
   ```

2. **Run the application:**
   ```powershell
   dotnet run
   ```

3. **Navigate to:** `https://localhost:5001`

4. **Sign in** with your Entra ID account

5. **Test features:**
   - Click "Show profile" to see user information
   - Click "Entra ID Groups" to see all groups

## Troubleshooting

### Authentication Issues
- Verify ClientId and TenantId are correct
- Check Redirect URI matches in App Registration
- Ensure user has permission to sign in to the app

### Graph API Errors
- **Insufficient privileges**: Grant Group.Read.All to Managed Identity
- **401 Unauthorized**: Check token acquisition is working
- **403 Forbidden**: Verify API permissions are granted

### Local Development
- Run `az login` before starting the app
- Ensure your account has Graph API permissions
- Use Developer Mode in appsettings.Development.json

## Key Code Highlights

### Program.cs - Authentication Setup
```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddInMemoryTokenCaches();
```

### Groups.razor - Fetching Groups
```csharp
var result = await GraphServiceClient.Groups
    .Request()
    .Select("id,displayName,description,mail,mailEnabled,securityEnabled")
    .Top(999)
    .OrderBy("displayName")
    .GetAsync();
```

## Resources

- [Microsoft Identity Web Documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [Azure Managed Identity](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)
- [Microsoft Graph API Reference](https://learn.microsoft.com/en-us/graph/api/overview)
- [Blazor Server Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)

## License

This project is provided as-is for educational and demonstration purposes.

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review Azure deployment guide
3. Consult Microsoft documentation links above
