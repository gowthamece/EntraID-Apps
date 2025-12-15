# ---------------------------
# CONFIGURATION SECTION
# ---------------------------

$tenantId = "b8f1747e-93a5-4b5b-8abc-91ce417dd3d6"          # Required if you want to explicitly target a directory
$webAppName = "blazorapp-dev01"

# Microsoft Graph details (static values)
$graphAppId = "00000003-0000-0000-c000-000000000000"
$groupReadAllRoleId = "5b567255-7703-4780-807c-7be8301ae99b" # Group.Read.All App Permission

# ---------------------------
# LOGIN AND SET CONTEXT
# ---------------------------

Write-Output "Logging into Azure..."

# If you want to force login to a specific Azure AD tenant
az login --tenant $tenantId

Write-Output "Setting default tenant context..."
az account set --subscription (az account show --query id -o tsv)

# ---------------------------
# CONNECT TO MICROSOFT GRAPH
# ---------------------------

Write-Output "Connecting to Microsoft Graph..."
Connect-MgGraph -TenantId $tenantId -Scopes "Application.ReadWrite.All"

# ---------------------------
# RESOLVE SERVICE PRINCIPALS
# ---------------------------

Write-Output "Fetching Managed Identity service principal..."

$managedIdentitySp = Get-MgServicePrincipal -Filter "displayName eq '$webAppName'"

if (-not $managedIdentitySp) {
    Write-Error "❌ Managed Identity not found. Ensure the Managed Identity is enabled on the Web App."
    exit
}

Write-Output "✔ Managed Identity Found: $($managedIdentitySp.Id)"

Write-Output "Fetching Microsoft Graph service principal..."

$graphSp = Get-MgServicePrincipal -Filter "appId eq '$graphAppId'"

if (-not $graphSp) {
    Write-Error "❌ Microsoft Graph service principal could not be retrieved."
    exit
}

# ---------------------------
# ASSIGN APPLICATION PERMISSION
# ---------------------------

Write-Output "Assigning 'Group.Read.All' Application permission to the Managed Identity..."

New-MgServicePrincipalAppRoleAssignment `
    -ServicePrincipalId $managedIdentitySp.Id `
    -PrincipalId $managedIdentitySp.Id `
    -ResourceId $graphSp.Id `
    -AppRoleId $groupReadAllRoleId

Write-Output "✔ Permission successfully assigned."

Write-Output "Restart your Azure Web App so the Managed Identity token is refreshed."

