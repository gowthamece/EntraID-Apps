# Infra deployment (Azure Functions Flex Consumption)

This folder contains a Bicep template to deploy:

- Azure Function App (Linux, Flex Consumption)
- App Service plan (`FC1` / Flex Consumption)
- Storage account
- Application Insights
- App settings for Entra token validation used by the function code

## Files

- `main.bicep`: main infrastructure template
- `main.parameters.dev.json`: sample dev parameter values
- `main.parameters.prod.json`: sample prod parameter values

## Prerequisites

- Azure CLI logged in (`az login`)
- Existing target resource group

## Validate template

```bash
az deployment group validate \
  --resource-group <rg-name> \
  --template-file main.bicep \
  --parameters @main.parameters.dev.json
```

## Deploy template

```bash
az deployment group create \
  --resource-group <rg-name> \
  --template-file main.bicep \
  --parameters @main.parameters.dev.json
```

## Notes

- `functionAppName` must be globally unique.
- `storageAccountName` must be globally unique, 3-24 chars, lowercase letters and numbers only.
- App settings in Azure use `EntraId__*` (double underscore) to map to `EntraId:*` configuration keys in .NET.

## GitHub Actions deployment

Workflow file: `.github/workflows/deploy-functionapp-bicep.yml`

### Azure AD app registration (OIDC)

The workflow uses OpenID Connect (OIDC) federated credentials. Before the workflow can authenticate, you must configure the app registration in Azure AD:

1. Create an app registration in Azure AD (Entra ID).
2. Grant the app registration the **Contributor** role on the target subscription or resource groups.
3. Add two federated credentials to the app registration, one per GitHub environment:

   | Field | Value (dev) | Value (prod) |
   |---|---|---|
   | Issuer | `https://token.actions.githubusercontent.com` | `https://token.actions.githubusercontent.com` |
   | Subject identifier | `repo:gowthamece/EntraID-Apps:environment:dev` | `repo:gowthamece/EntraID-Apps:environment:prod` |
   | Audience | `api://AzureADTokenExchange` | `api://AzureADTokenExchange` |

   > **Important:** The subject identifier must match the GitHub environment name exactly. Replace `gowthamece/EntraID-Apps` with your actual `<owner>/<repository>` if you fork or rename this repository.

### Required repository settings

- Variable: `AZURE_RESOURCE_GROUP_DEV`
- Variable: `AZURE_RESOURCE_GROUP_PROD`
- Secret: `AZURE_CLIENT_ID` — the Application (client) ID of the app registration above
- Secret: `AZURE_TENANT_ID` — the Directory (tenant) ID of the app registration above
- Secret: `AZURE_SUBSCRIPTION_ID` — the Azure subscription ID (GUID) where resources will be deployed

> **Troubleshooting:** If the workflow fails with *"The subscription … doesn't exist in cloud 'AzureCloud'"*, verify that `AZURE_SUBSCRIPTION_ID` contains the correct subscription GUID and that the app registration has been granted access to that subscription or target resource group. The workflow logs in with `allow-no-subscriptions: true` so resource-group-scoped role assignments can still authenticate before the `az deployment group` commands target the subscription explicitly.

The workflow is restricted to runs where `github.actor` is `gowthamece`.

Approval gate setup:

- Create GitHub Environment: `dev` (no required reviewers; auto deploy)
- Create GitHub Environment: `prod`
- In environment protection rules, enable `Required reviewers`
- Add at least one reviewer (for example, your account `gowthamece`)

Workflow behavior:

- `deploy-dev`: runs automatically first, using `main.parameters.dev.json`
- `deploy-prod`: runs only after dev succeeds and waits for `prod` approval, using `main.parameters.prod.json`
