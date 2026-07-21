param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$WebAppName,

    [Parameter(Mandatory = $true)]
    [string]$FunctionApiAppId,

    [string]$DefaultScope = "api://2766a7d4-1ac2-4d65-be3f-7e6478edd00a/.default",

    [string]$DelegatedScope = "api://2766a7d4-1ac2-4d65-be3f-7e6478edd00a/access_as_user",

    [string]$ApplicationRoleValue = "access_as_application",

    [string]$SubscriptionId = "ba17a19a-87ca-4e42-9bdf-9ec5da7f5a8b"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "=== Managed Identity Permission Assignment ==="
Write-Host "WebApp: $WebAppName"
Write-Host "Function API AppId: $FunctionApiAppId"
Write-Host "Requested scope values:"
Write-Host "  - $DefaultScope"
Write-Host "  - $DelegatedScope"
Write-Host "Target subscription: $SubscriptionId"
Write-Host ""

# 0) Authenticate interactively and set target subscription.
Write-Host "Signing in to Azure interactively..."
az login | Out-Null

Write-Host "Setting active subscription to '$SubscriptionId'..."
az account set --subscription $SubscriptionId

$activeSubscriptionId = az account show --query id --output tsv
if ($activeSubscriptionId -ne $SubscriptionId) {
    throw "Failed to switch Azure CLI context to subscription '$SubscriptionId'. Current: '$activeSubscriptionId'."
}

Write-Host "Azure CLI context is set to subscription: $activeSubscriptionId"
Write-Host ""

# 1) Ensure system-assigned managed identity is enabled.
Write-Host "Ensuring system-assigned managed identity is enabled on web app..."
az webapp identity assign --resource-group $ResourceGroupName --name $WebAppName | Out-Null

$managedIdentityPrincipalId = az webapp identity show `
    --resource-group $ResourceGroupName `
    --name $WebAppName `
    --query principalId `
    --output tsv

if ([string]::IsNullOrWhiteSpace($managedIdentityPrincipalId)) {
    throw "Unable to resolve managed identity principalId for web app '$WebAppName'."
}

Write-Host "Managed Identity PrincipalId: $managedIdentityPrincipalId"

# 2) Resolve Function API service principal and app registration metadata.
$apiSpObjectId = az ad sp show --id $FunctionApiAppId --query id --output tsv
if ([string]::IsNullOrWhiteSpace($apiSpObjectId)) {
    throw "Unable to resolve service principal for Function API appId '$FunctionApiAppId'."
}

$apiAppObjectId = az ad app show --id $FunctionApiAppId --query id --output tsv
if ([string]::IsNullOrWhiteSpace($apiAppObjectId)) {
    throw "Unable to resolve app registration for Function API appId '$FunctionApiAppId'."
}

Write-Host "Function API ServicePrincipal ObjectId: $apiSpObjectId"
Write-Host "Function API App ObjectId: $apiAppObjectId"
Write-Host ""

# 3) Explain .default behavior.
Write-Host "INFO: '$DefaultScope' is not an assignable permission object."
Write-Host "INFO: '.default' means 'issue token with all already-consented app permissions for this resource'."
Write-Host ""

# 4) Check delegated scope in API app registration.
$delegatedScopeValue = $DelegatedScope
if ($delegatedScopeValue.Contains('/')) {
    $delegatedScopeValue = $delegatedScopeValue.Substring($delegatedScopeValue.LastIndexOf('/') + 1)
}

$scopeId = az ad app show `
    --id $FunctionApiAppId `
    --query "api.oauth2PermissionScopes[?value=='$delegatedScopeValue'].id | [0]" `
    --output tsv

if ([string]::IsNullOrWhiteSpace($scopeId)) {
    Write-Warning "Delegated scope '$delegatedScopeValue' was not found on Function API app registration."
}
else {
    Write-Host "Found delegated scope '$delegatedScopeValue' with Id: $scopeId"
}

Write-Warning "Managed identity cannot be granted delegated scopes (scp) like '$delegatedScopeValue'."
Write-Warning "Delegated scopes require user-delegated auth flows."
Write-Host ""

# 5) Assign application role for app-only tokens (roles claim).
$appRoleId = az ad sp show `
    --id $FunctionApiAppId `
    --query "appRoles[?value=='$ApplicationRoleValue' && contains(allowedMemberTypes, 'Application')].id | [0]" `
    --output tsv

if ([string]::IsNullOrWhiteSpace($appRoleId)) {
    throw "Application role '$ApplicationRoleValue' not found on Function API service principal. Add an app role for application callers first."
}

Write-Host "Resolved application role '$ApplicationRoleValue' with Id: $appRoleId"

$existingCount = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$managedIdentityPrincipalId/appRoleAssignments" `
    --query "value[?resourceId=='$apiSpObjectId' && appRoleId=='$appRoleId'] | length(@)" `
    --output tsv

if ($existingCount -eq "0") {
    Write-Host "Assigning app role '$ApplicationRoleValue' to managed identity..."

    $payload = @{
        principalId = $managedIdentityPrincipalId
        resourceId  = $apiSpObjectId
        appRoleId   = $appRoleId
    } | ConvertTo-Json -Compress

    az rest `
        --method POST `
        --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$managedIdentityPrincipalId/appRoleAssignments" `
        --headers "Content-Type=application/json" `
        --body $payload | Out-Null

    Write-Host "App role assignment completed."
}
else {
    Write-Host "App role assignment already exists. No change made."
}

Write-Host ""
Write-Host "Completed with notes:"
Write-Host "  - '.default' is used in token requests, not direct assignment."
Write-Host "  - Delegated scope '$delegatedScopeValue' cannot be assigned to managed identity."
Write-Host "  - Application role '$ApplicationRoleValue' was validated/assigned for app-only authorization."
