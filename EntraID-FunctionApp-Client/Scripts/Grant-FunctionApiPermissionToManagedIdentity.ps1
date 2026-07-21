param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$WebAppName,

    [Parameter(Mandatory = $true)]
    [string]$FunctionApiAppId,

    [string]$ApiAppRoleValue = "access_as_application"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Ensuring system-assigned managed identity is enabled for web app '$WebAppName'..."
az webapp identity assign --resource-group $ResourceGroupName --name $WebAppName | Out-Null

Write-Host "Resolving system-assigned managed identity for web app '$WebAppName'..."
$managedIdentityPrincipalId = az webapp identity show `
    --resource-group $ResourceGroupName `
    --name $WebAppName `
    --query principalId `
    --output tsv

if ([string]::IsNullOrWhiteSpace($managedIdentityPrincipalId)) {
    throw "Managed identity principalId was not found. Ensure system-assigned identity is enabled on the web app."
}

Write-Host "Resolving Function API service principal for appId '$FunctionApiAppId'..."
$functionApiSpObjectId = az ad sp show --id $FunctionApiAppId --query id --output tsv
if ([string]::IsNullOrWhiteSpace($functionApiSpObjectId)) {
    throw "Could not resolve Function API service principal from appId '$FunctionApiAppId'."
}

Write-Host "Resolving application role '$ApiAppRoleValue' on the Function API service principal..."
$appRoleId = az ad sp show `
    --id $FunctionApiAppId `
    --query "appRoles[?value=='$ApiAppRoleValue' && contains(allowedMemberTypes, 'Application')].id | [0]" `
    --output tsv

if ([string]::IsNullOrWhiteSpace($appRoleId)) {
    throw "App role '$ApiAppRoleValue' not found. Expose an application app role on the Function API app registration first."
}

Write-Host "Checking for existing app role assignment..."
$existingCount = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$managedIdentityPrincipalId/appRoleAssignments" `
    --query "value[?resourceId=='$functionApiSpObjectId' && appRoleId=='$appRoleId'] | length(@)" `
    --output tsv

if ($existingCount -eq "0") {
    Write-Host "Assigning app role '$ApiAppRoleValue' to managed identity..."
    $payload = @{
        principalId = $managedIdentityPrincipalId
        resourceId  = $functionApiSpObjectId
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

Write-Host "Done."
Write-Host "Note: Managed identity obtains app-only tokens. Ensure your Function API accepts the 'roles' claim value '$ApiAppRoleValue'."
