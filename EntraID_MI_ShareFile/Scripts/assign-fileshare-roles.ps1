# =============================================================================
# Azure File Share Role Assignment Script
# =============================================================================
# This script assigns the required roles to download files from Azure File Share
# using Managed Identity with DefaultAzureCredential and ShareTokenIntent.Backup
# =============================================================================

# -------------------------
# Configuration Parameters
# -------------------------
param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$StorageAccountName,
    
    [Parameter(Mandatory=$false)]
    [string]$GroupObjectId = "{your_object_id}",  # Dev-Team group
    
    [Parameter(Mandatory=$false)]
    [string]$GroupDisplayName = "{your_display_name}"
)

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Azure File Share Role Assignment Script" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# -------------------------
# Login and Set Subscription
# -------------------------
Write-Host "Step 1: Checking Azure CLI login status..." -ForegroundColor Yellow

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in. Initiating Azure login..." -ForegroundColor Yellow
    az login
}

Write-Host "Setting subscription to: $SubscriptionId" -ForegroundColor Yellow
az account set --subscription $SubscriptionId

Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host ""

# -------------------------
# Validate Storage Account Exists
# -------------------------
Write-Host "Step 2: Validating storage account exists..." -ForegroundColor Yellow

$storageAccount = az storage account show `
    --name $StorageAccountName `
    --resource-group $ResourceGroupName `
    2>$null | ConvertFrom-Json

if (-not $storageAccount) {
    Write-Host "ERROR: Storage account '$StorageAccountName' not found in resource group '$ResourceGroupName'" -ForegroundColor Red
    exit 1
}

Write-Host "Storage account found: $($storageAccount.name)" -ForegroundColor Green
Write-Host ""

# -------------------------
# Define the scope
# -------------------------
$scope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Storage/storageAccounts/$StorageAccountName"

Write-Host "Scope: $scope" -ForegroundColor Gray
Write-Host ""

# -------------------------
# Role Assignment for Dev-Team Group
# -------------------------
Write-Host "Step 3: Assigning 'Storage File Data Privileged Reader' role to group '$GroupDisplayName'..." -ForegroundColor Yellow
Write-Host "        Group Object ID: $GroupObjectId" -ForegroundColor Gray

# Storage File Data Privileged Reader - Required for ShareTokenIntent.Backup
# This role allows read access to Azure File Shares using OAuth authentication
az role assignment create `
    --role "Storage File Data Privileged Reader" `
    --assignee-object-id $GroupObjectId `
    --assignee-principal-type "Group" `
    --scope $scope

if ($LASTEXITCODE -eq 0) {
    Write-Host "Successfully assigned 'Storage File Data Privileged Reader' role to group" -ForegroundColor Green
} else {
    Write-Host "Note: Role may already be assigned or there was an error" -ForegroundColor Yellow
}

Write-Host ""

# -------------------------
# Role Assignment for Current User (Visual Studio Credential)
# -------------------------
Write-Host "Step 4: Assigning 'Storage File Data Privileged Reader' role to current user for Visual Studio debugging..." -ForegroundColor Yellow

# Get current signed-in user
$currentUser = az ad signed-in-user show 2>$null | ConvertFrom-Json

if ($currentUser) {
    Write-Host "        Current User: $($currentUser.userPrincipalName)" -ForegroundColor Gray
    Write-Host "        User Object ID: $($currentUser.id)" -ForegroundColor Gray
    
    az role assignment create `
        --role "Storage File Data Privileged Reader" `
        --assignee-object-id $currentUser.id `
        --assignee-principal-type "User" `
        --scope $scope
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Successfully assigned 'Storage File Data Privileged Reader' role to current user" -ForegroundColor Green
    } else {
        Write-Host "Note: Role may already be assigned or there was an error" -ForegroundColor Yellow
    }
} else {
    Write-Host "WARNING: Could not retrieve current user info. Please assign the role manually." -ForegroundColor Yellow
}

Write-Host ""

# -------------------------
# Verify Role Assignments
# -------------------------
Write-Host "Step 5: Verifying role assignments..." -ForegroundColor Yellow

Write-Host ""
Write-Host "Role assignments for group '$GroupDisplayName':" -ForegroundColor Cyan
az role assignment list `
    --assignee $GroupObjectId `
    --scope $scope `
    --query "[].{Role:roleDefinitionName, Principal:principalName, Scope:scope}" `
    --output table

if ($currentUser) {
    Write-Host ""
    Write-Host "Role assignments for current user '$($currentUser.userPrincipalName)':" -ForegroundColor Cyan
    az role assignment list `
        --assignee $currentUser.id `
        --scope $scope `
        --query "[].{Role:roleDefinitionName, Principal:principalName, Scope:scope}" `
        --output table
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Role Assignment Complete!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "IMPORTANT NOTES:" -ForegroundColor Yellow
Write-Host "1. Role assignments may take up to 5 minutes to propagate." -ForegroundColor White
Write-Host "2. For Visual Studio debugging, ensure you are signed in with the same account" -ForegroundColor White
Write-Host "   in Visual Studio (Tools > Options > Azure Service Authentication)." -ForegroundColor White
Write-Host "3. The 'Storage File Data Privileged Reader' role is required because your code" -ForegroundColor White
Write-Host "   uses ShareTokenIntent.Backup for OAuth-based file share access." -ForegroundColor White
Write-Host ""
