targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Function App name.')
param functionAppName string

@description('Storage account name (3-24 lowercase alphanumeric).')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('Flex Consumption plan name.')
param appServicePlanName string = '${functionAppName}-plan'

@description('Application Insights resource name.')
param appInsightsName string = '${functionAppName}-appi'

@description('Maximum Flex Consumption instances.')
@minValue(40)
@maxValue(1000)
param maximumInstanceCount int = 100

@description('Memory per instance (MB). Allowed: 512, 2048, 4096.')
@allowed([
  512
  2048
  4096
])
param instanceMemoryMB int = 2048

@description('Entra tenant ID used by the function token validator.')
param entraTenantId string = '6e01b1f9-b1e5-4073-ac97-778069a0ad64'

@description('Entra app ID used by the function token validator.')
param entraAppId string = '84a651ee-de65-4753-ba10-f89389c9308d'

@description('Expected audience (resource app/client ID) for incoming access tokens.')
param entraAudience string = '2766a7d4-1ac2-4d65-be3f-7e6478edd00a'

@description('Required scope claim for incoming access tokens.')
param entraRequiredScope string = 'api://2766a7d4-1ac2-4d65-be3f-7e6478edd00a/access_as_user'

@description('Required app role claim for app-only tokens (managed identity/client credentials).')
param entraRequiredAppRole string = 'access_as_application'

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${listKeys(storage.id, storage.apiVersion).keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    IngestionMode: 'ApplicationInsights'
  }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    functionAppConfig: {
      runtime: {
        name: 'dotnet-isolated'
        version: '8.0'
      }
      scaleAndConcurrency: {
        maximumInstanceCount: maximumInstanceCount
        instanceMemoryMB: instanceMemoryMB
      }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'EntraId__TenantId'
          value: entraTenantId
        }
        {
          name: 'EntraId__AppId'
          value: entraAppId
        }
        {
          name: 'EntraId__Audience'
          value: entraAudience
        }
        {
          name: 'EntraId__RequiredScope'
          value: entraRequiredScope
        }
        {
          name: 'EntraId__RequiredAppRole'
          value: entraRequiredAppRole
        }
      ]
    }
  }
}

output functionAppResourceId string = functionApp.id
output functionAppDefaultHostName string = functionApp.properties.defaultHostName