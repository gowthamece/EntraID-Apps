# Azure Deployment Guide - Blazor Server with Managed Identity

## Overview
This guide explains how to deploy your Blazor Server application to Azure Web App with Managed Identity to access Microsoft Graph API without using client secrets.

## Prerequisites
- Azure subscription
- Azure CLI or Azure Portal access
- Application already registered in Microsoft Entra ID

## Step 1: Create Azure Web App

### Using Azure Portal:
1. Go to Azure Portal (https://portal.azure.com)
2. Click "Create a resource" → "Web App"
3. Fill in the details:
   - **Subscription**: Select your subscription
   - **Resource Group**: Create new or select existing
   - **Name**: Choose a unique name (e.g., `myapp-blazor-server`)
   - **Runtime stack**: .NET 10
   - **Operating System**: Windows or Linux
   - **Region**: Select your preferred region
4. Click "Review + create" → "Create"

### Using Azure CLI:
```bash
az webapp create \
  --name myapp-blazor-server \
  --resource-group myResourceGroup \
  --plan myAppServicePlan \
  --runtime "DOTNET:10"
```

## Step 2: Enable Managed Identity

### Using Azure Portal:
1. Navigate to your Web App in Azure Portal
2. Go to **Settings** → **Identity**
3. Under **System assigned** tab:
   - Set **Status** to **On**
   - Click **Save**
4. Copy the **Object (principal) ID** - you'll need this

### Using Azure CLI:
```bash
az webapp identity assign \
  --name myapp-blazor-server \
  --resource-group myResourceGroup
```

Note the `principalId` from the output.

## Step 3: Grant Microsoft Graph Permissions to Managed Identity

You need to grant the Managed Identity the `Group.Read.All` permission in Microsoft Graph.

### Using PowerShell (Azure Cloud Shell or local):

```powershell
# Install required module if not already installed
Install-Module Microsoft.Graph -Scope CurrentUser

# Connect to Microsoft Graph
Connect-MgGraph -Scopes "Application.Read.All", "AppRoleAssignment.ReadWrite.All"

# Get the Managed Identity Object ID (from Step 2)
$managedIdentityObjectId = "YOUR-MANAGED-IDENTITY-OBJECT-ID"

# Get Microsoft Graph Service Principal
$graphSP = Get-MgServicePrincipal -Filter "appId eq '00000003-0000-0000-c000-000000000000'"

# Get the Group.Read.All permission ID
$groupReadAllPermission = $graphSP.AppRoles | Where-Object { $_.Value -eq "Group.Read.All" }

# Assign the permission to the Managed Identity
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityObjectId `
    -PrincipalId $managedIdentityObjectId `
    -ResourceId $graphSP.Id `
    -AppRoleId $groupReadAllPermission.Id
```

### Alternative: Using Microsoft Graph API directly:

```bash
# Get access token
az login
$token = (az account get-access-token --resource https://graph.microsoft.com --query accessToken -o tsv)

# Get Graph Service Principal
$graphSP = Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/servicePrincipals?`$filter=appId eq '00000003-0000-0000-c000-000000000000'" `
    -Headers @{Authorization = "Bearer $token"}

# Assign permission (replace with your managed identity object ID)
$body = @{
    principalId = "YOUR-MANAGED-IDENTITY-OBJECT-ID"
    resourceId = $graphSP.value[0].id
    appRoleId = "5b567255-7703-4780-807c-7be8301ae99b"  # Group.Read.All
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
    -Uri "https://graph.microsoft.com/v1.0/servicePrincipals/YOUR-MANAGED-IDENTITY-OBJECT-ID/appRoleAssignments" `
    -Headers @{Authorization = "Bearer $token"; "Content-Type" = "application/json"} `
    -Body $body
```

## Step 4: Configure Application Settings in Azure Web App

### Using Azure Portal:
1. Go to your Web App → **Settings** → **Configuration**
2. Add the following Application Settings:

| Name | Value |
|------|-------|
| `AzureAd__Instance` | `https://login.microsoftonline.com/` |
| `AzureAd__Domain` | `yourtenant.onmicrosoft.com` |
| `AzureAd__TenantId` | `YOUR-TENANT-ID` |
| `AzureAd__ClientId` | `YOUR-CLIENT-ID` |
| `AzureAd__CallbackPath` | `/signin-oidc` |
| `MicrosoftGraph__BaseUrl` | `https://graph.microsoft.com/v1.0` |
| `MicrosoftGraph__Scopes` | `Group.Read.All` |

3. Click **Save**

### Using Azure CLI:
```bash
az webapp config appsettings set \
  --name myapp-blazor-server \
  --resource-group myResourceGroup \
  --settings \
    AzureAd__Instance="https://login.microsoftonline.com/" \
    AzureAd__Domain="yourtenant.onmicrosoft.com" \
    AzureAd__TenantId="YOUR-TENANT-ID" \
    AzureAd__ClientId="YOUR-CLIENT-ID" \
    AzureAd__CallbackPath="/signin-oidc" \
    MicrosoftGraph__BaseUrl="https://graph.microsoft.com/v1.0" \
    MicrosoftGraph__Scopes="Group.Read.All"
```

## Step 5: Update Entra ID App Registration

1. Go to Azure Portal → **Microsoft Entra ID** → **App registrations**
2. Select your application
3. Go to **Authentication**
4. Under **Platform configurations** → **Web**:
   - Add Redirect URI: `https://YOUR-APP-NAME.azurewebsites.net/signin-oidc`
   - Ensure **ID tokens** is checked
5. Click **Save**

## Step 6: Deploy Your Application

### Option 1: Visual Studio
1. Right-click on your project → **Publish**
2. Select **Azure** → **Azure App Service (Windows/Linux)**
3. Select your Web App
4. Click **Publish**

### Option 2: Azure CLI
```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Create a zip file
Compress-Archive -Path ./publish/* -DestinationPath ./app.zip

# Deploy to Azure
az webapp deployment source config-zip \
  --name myapp-blazor-server \
  --resource-group myResourceGroup \
  --src ./app.zip
```

### Option 3: GitHub Actions (CI/CD)
Create `.github/workflows/azure-webapps-dotnet.yml`:

```yaml
name: Deploy to Azure Web App

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build-and-deploy:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v4

    - name: Set up .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'

    - name: Build
      run: dotnet build --configuration Release

    - name: Publish
      run: dotnet publish -c Release -o ${{env.DOTNET_ROOT}}/myapp

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'myapp-blazor-server'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ${{env.DOTNET_ROOT}}/myapp
```

## Step 7: Verify Deployment

1. Navigate to `https://YOUR-APP-NAME.azurewebsites.net`
2. Click "Log in" to authenticate
3. After signing in, navigate to the "Entra ID Groups" page
4. You should see all groups displayed (the app uses Managed Identity to access Graph API)

## Troubleshooting

### Issue: "Insufficient privileges to complete the operation"
**Solution**: Ensure the Managed Identity has been granted `Group.Read.All` permission (Step 3)

### Issue: Authentication loop or redirect errors
**Solution**: 
- Verify the Redirect URI is correctly configured in Entra ID App Registration
- Check that `CallbackPath` matches in appsettings.json and App Registration

### Issue: "AADSTS700016: Application not found in the directory"
**Solution**: Verify `ClientId` and `TenantId` are correct in application settings

### Issue: Groups not loading
**Solution**: 
- Check application logs in Azure Portal → Web App → **Monitoring** → **Log stream**
- Verify Managed Identity has correct permissions
- Test locally first with user credentials

## Important Notes

### Managed Identity vs Client Secret
- **Managed Identity**: No secrets to manage, automatic credential rotation, works only in Azure
- **Client Secret**: Requires secret management, works everywhere, but less secure

### Local Development
For local development, the app will use your Azure credentials from Visual Studio or Azure CLI:
```bash
# Login with Azure CLI
az login

# Or use Visual Studio's Azure Service Authentication
```

The code automatically detects the environment:
- **Azure**: Uses Managed Identity
- **Local**: Uses developer credentials from Azure CLI or Visual Studio

### Security Best Practices
1. Never store client secrets in source control
2. Use Managed Identity in Azure environments
3. Limit Graph API permissions to only what's needed
4. Enable diagnostic logging for security auditing
5. Use Azure Key Vault for any additional secrets

## Additional Resources
- [Microsoft Identity Web documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [Managed Identity overview](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)
- [Microsoft Graph permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference)
