# Quick Start Guide - Azure Deployment

## Prerequisites
- Azure subscription
- Application registered in Microsoft Entra ID
- PowerShell or Azure CLI installed

## 5-Minute Deployment

### Step 1: Create Web App (2 min)
```powershell
az webapp create `
  --name myapp-blazor-entra `
  --resource-group myResourceGroup `
  --plan myAppServicePlan `
  --runtime "DOTNET:10"
```

### Step 2: Enable Managed Identity (1 min)
```powershell
$identity = az webapp identity assign `
  --name myapp-blazor-entra `
  --resource-group myResourceGroup | ConvertFrom-Json

$managedIdentityId = $identity.principalId
Write-Host "Managed Identity ID: $managedIdentityId"
```

### Step 3: Grant Graph Permissions (2 min)
```powershell
# Connect to Microsoft Graph
Connect-MgGraph -Scopes "Application.Read.All","AppRoleAssignment.ReadWrite.All"

# Get Graph Service Principal
$graphSP = Get-MgServicePrincipal -Filter "appId eq '00000003-0000-0000-c000-000000000000'"

# Get Group.Read.All permission
$permission = $graphSP.AppRoles | Where-Object { $_.Value -eq "Group.Read.All" }

# Assign permission to Managed Identity
New-MgServicePrincipalAppRoleAssignment `
  -ServicePrincipalId $managedIdentityId `
  -PrincipalId $managedIdentityId `
  -ResourceId $graphSP.Id `
  -AppRoleId $permission.Id

Write-Host "✅ Permissions granted successfully!"
```

### Step 4: Configure App Settings (1 min)
```powershell
az webapp config appsettings set `
  --name myapp-blazor-entra `
  --resource-group myResourceGroup `
  --settings `
    AzureAd__Instance="https://login.microsoftonline.com/" `
    AzureAd__Domain="YOUR-TENANT.onmicrosoft.com" `
    AzureAd__TenantId="YOUR-TENANT-ID" `
    AzureAd__ClientId="YOUR-CLIENT-ID" `
    AzureAd__CallbackPath="/signin-oidc" `
    MicrosoftGraph__BaseUrl="https://graph.microsoft.com/v1.0" `
    MicrosoftGraph__Scopes="Group.Read.All"
```

### Step 5: Update App Registration Redirect URI
1. Go to Azure Portal → Entra ID → App Registrations
2. Select your app → Authentication
3. Add redirect URI: `https://myapp-blazor-entra.azurewebsites.net/signin-oidc`
4. Save

### Step 6: Deploy Application
```powershell
# Publish the app
dotnet publish -c Release -o ./publish

# Create zip
Compress-Archive -Path ./publish/* -DestinationPath ./app.zip -Force

# Deploy
az webapp deployment source config-zip `
  --name myapp-blazor-entra `
  --resource-group myResourceGroup `
  --src ./app.zip

Write-Host "✅ Deployment complete!"
Write-Host "🌐 Visit: https://myapp-blazor-entra.azurewebsites.net"
```

## Verification

1. Navigate to your app URL
2. Click "Log in"
3. Sign in with Entra ID credentials
4. Navigate to "Entra ID Groups"
5. Verify groups are displayed

## Troubleshooting Quick Fixes

### Problem: "Insufficient privileges"
```powershell
# Re-run Step 3 to grant permissions
```

### Problem: Authentication loop
```powershell
# Verify redirect URI matches exactly
az ad app show --id YOUR-CLIENT-ID --query "web.redirectUris"
```

### Problem: Groups not loading
```powershell
# Check app logs
az webapp log tail --name myapp-blazor-entra --resource-group myResourceGroup
```

## Environment Variables Reference

| Variable | Example Value |
|----------|--------------|
| `AzureAd__Instance` | `https://login.microsoftonline.com/` |
| `AzureAd__Domain` | `contoso.onmicrosoft.com` |
| `AzureAd__TenantId` | `12345678-1234-1234-1234-123456789012` |
| `AzureAd__ClientId` | `87654321-4321-4321-4321-210987654321` |
| `AzureAd__CallbackPath` | `/signin-oidc` |
| `MicrosoftGraph__BaseUrl` | `https://graph.microsoft.com/v1.0` |
| `MicrosoftGraph__Scopes` | `Group.Read.All` |

## All-in-One PowerShell Script

Save as `deploy.ps1`:

```powershell
# Configuration
$appName = "myapp-blazor-entra"
$resourceGroup = "myResourceGroup"
$tenantDomain = "YOUR-TENANT.onmicrosoft.com"
$tenantId = "YOUR-TENANT-ID"
$clientId = "YOUR-CLIENT-ID"

Write-Host "🚀 Starting deployment..." -ForegroundColor Cyan

# 1. Create Web App
Write-Host "📦 Creating Web App..." -ForegroundColor Yellow
az webapp create --name $appName --resource-group $resourceGroup --plan myAppServicePlan --runtime "DOTNET:10"

# 2. Enable Managed Identity
Write-Host "🔐 Enabling Managed Identity..." -ForegroundColor Yellow
$identity = az webapp identity assign --name $appName --resource-group $resourceGroup | ConvertFrom-Json
$managedIdentityId = $identity.principalId
Write-Host "   Managed Identity ID: $managedIdentityId" -ForegroundColor Green

# 3. Grant Graph Permissions
Write-Host "📊 Granting Graph API permissions..." -ForegroundColor Yellow
Connect-MgGraph -Scopes "Application.Read.All","AppRoleAssignment.ReadWrite.All" -NoWelcome
$graphSP = Get-MgServicePrincipal -Filter "appId eq '00000003-0000-0000-c000-000000000000'"
$permission = $graphSP.AppRoles | Where-Object { $_.Value -eq "Group.Read.All" }
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphSP.Id -AppRoleId $permission.Id
Write-Host "   ✅ Permissions granted" -ForegroundColor Green

# 4. Configure App Settings
Write-Host "⚙️  Configuring App Settings..." -ForegroundColor Yellow
az webapp config appsettings set --name $appName --resource-group $resourceGroup --settings `
    AzureAd__Instance="https://login.microsoftonline.com/" `
    AzureAd__Domain="$tenantDomain" `
    AzureAd__TenantId="$tenantId" `
    AzureAd__ClientId="$clientId" `
    AzureAd__CallbackPath="/signin-oidc" `
    MicrosoftGraph__BaseUrl="https://graph.microsoft.com/v1.0" `
    MicrosoftGraph__Scopes="Group.Read.All"

# 5. Deploy Application
Write-Host "🚢 Deploying application..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./app.zip -Force
az webapp deployment source config-zip --name $appName --resource-group $resourceGroup --src ./app.zip

Write-Host ""
Write-Host "✅ Deployment Complete!" -ForegroundColor Green
Write-Host "🌐 Your app is live at: https://$appName.azurewebsites.net" -ForegroundColor Cyan
Write-Host ""
Write-Host "⚠️  Don't forget to add redirect URI to App Registration:" -ForegroundColor Yellow
Write-Host "   https://$appName.azurewebsites.net/signin-oidc" -ForegroundColor White
```

Run with:
```powershell
.\deploy.ps1
```

## Next Steps

1. ✅ Update redirect URI in App Registration
2. ✅ Test login at your app URL
3. ✅ Verify groups page works
4. 📊 Monitor in Application Insights
5. 🔒 Review security settings

For detailed documentation, see **AZURE_DEPLOYMENT.md**
