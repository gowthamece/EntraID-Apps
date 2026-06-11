# ---------------------------
# CONFIGURATION SECTION
# ---------------------------

$tenantId = "b8f1747e-93a5-4b5b-8abc-91ce417dd3d6"
$webAppName = "blazorapp-dev01"

# Microsoft Graph details
$graphAppId = "00000003-0000-0000-c000-000000000000"

# Microsoft Graph App Role IDs
$directoryReadAllRoleId = "7ab1d382-f21e-4acd-a863-ba3e13f7da61" # Directory.Read.All
$groupReadAllRoleId     = "5b567255-7703-4780-807c-7be8301ae99b" # Group.Read.All
$userReadAllRoleId      = "df021288-bdef-4463-88db-98f22de89214" # User.Read.All

# ---------------------------
# LOGIN
# ---------------------------

Write-Output "Logging into Azure..."
az login --tenant $tenantId

# ---------------------------
# RESOLVE SERVICE PRINCIPALS USING REST ONLY
# ---------------------------

Write-Output "Fetching Managed Identity service principal via REST..."
$managedIdentityResponse = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals?`$filter=displayName eq '$webAppName'" `
    --headers "Content-Type=application/json" | ConvertFrom-Json

$managedIdentitySp = $managedIdentityResponse.value | Select-Object -First 1

if (-not $managedIdentitySp) {
    Write-Error "Managed Identity not found. Ensure the Managed Identity is enabled on the Web App."
    exit
}

Write-Output "Managed Identity Found: $($managedIdentitySp.id)"

Write-Output "Fetching Microsoft Graph service principal via REST..."
$graphSpResponse = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals?`$filter=appId eq '$graphAppId'" `
    --headers "Content-Type=application/json" | ConvertFrom-Json

$graphSp = $graphSpResponse.value | Select-Object -First 1

if (-not $graphSp) {
    Write-Error "Microsoft Graph service principal could not be retrieved."
    exit
}

Write-Output "Microsoft Graph Service Principal Found: $($graphSp.id)"

# ---------------------------
# ASSIGN APPLICATION PERMISSIONS USING REST ONLY
# ---------------------------

$graphPermissions = @(
    @{ Name = "Directory.Read.All"; AppRoleId = $directoryReadAllRoleId },
    @{ Name = "Group.Read.All";     AppRoleId = $groupReadAllRoleId },
    @{ Name = "User.Read.All";      AppRoleId = $userReadAllRoleId }
)

foreach ($permission in $graphPermissions) {
    Write-Output "Assigning '$($permission.Name)' application permission to the Managed Identity..."

    $body = @{
        principalId = $managedIdentitySp.id
        resourceId  = $graphSp.id
        appRoleId   = $permission.AppRoleId
    } | ConvertTo-Json -Compress

    az rest `
        --method POST `
        --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$($managedIdentitySp.id)/appRoleAssignments" `
        --headers "Content-Type=application/json" `
        --body $body | Out-Null

    Write-Output "Assigned '$($permission.Name)' successfully."
}

Write-Output "All requested Microsoft Graph API permissions have been assigned."
Write-Output "Restart your Azure Web App so the Managed Identity token is refreshed."
