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

Required repository settings:

- Variable: `AZURE_RESOURCE_GROUP_DEV`
- Variable: `AZURE_RESOURCE_GROUP_PROD`
- Secret: `AZURE_CLIENT_ID`
- Secret: `AZURE_TENANT_ID`
- Secret: `AZURE_SUBSCRIPTION_ID`

The workflow is restricted to runs where `github.actor` is `gowthamece`.

Approval gate setup:

- Create GitHub Environment: `dev` (no required reviewers; auto deploy)
- Create GitHub Environment: `prod`
- In environment protection rules, enable `Required reviewers`
- Add at least one reviewer (for example, your account `gowthamece`)

Workflow behavior:

- `deploy-dev`: runs automatically first, using `main.parameters.dev.json`
- `deploy-prod`: runs only after dev succeeds and waits for `prod` approval, using `main.parameters.prod.json`
