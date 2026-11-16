// Site Inspection Tracker - Azure infrastructure.
//
// Deploys into an existing resource group:
//   * Linux App Service plan + Web App running the ASP.NET Core 8 API
//   * Azure SQL logical server + database (connection string injected into the Web App)
//   * Azure Static Web App hosting the React client
//
// Usage (see README "Deploying to Azure"):
//   az deployment group create -g <rg> -f infra/main.bicep -p infra/main.bicepparam

targetScope = 'resourceGroup'

@description('Short name used to derive resource names (lowercase letters and digits).')
@minLength(3)
@maxLength(12)
param appName string = 'siteinspect'

@description('Region for the App Service and Azure SQL resources.')
param location string = resourceGroup().location

@description('Region for the Static Web App (only a subset of regions host Static Web Apps).')
@allowed([
  'centralus'
  'eastus2'
  'eastasia'
  'westeurope'
  'westus2'
])
param staticWebAppLocation string = 'westeurope'

@description('App Service plan SKU. B1 is the smallest tier that supports Always On.')
@allowed([
  'F1'
  'B1'
  'B2'
  'S1'
  'P0v3'
  'P1v3'
])
param appServicePlanSku string = 'B1'

@description('Azure SQL Database SKU name (e.g. Basic, S0, GP_S_Gen5_1).')
param sqlDatabaseSkuName string = 'Basic'

@description('Azure SQL Database SKU tier matching sqlDatabaseSkuName (e.g. Basic, Standard, GeneralPurpose).')
param sqlDatabaseSkuTier string = 'Basic'

@description('SQL administrator login for the logical server.')
param sqlAdministratorLogin string = 'sqladminuser'

@description('SQL administrator password. Supply at deploy time; never commit it.')
@secure()
param sqlAdministratorPassword string

@description('Seed the database with demo sites, inspections and findings on first start.')
param seedSampleData bool = true

@description('Expose Swagger UI in the deployed API.')
param enableSwagger bool = true

@description('Tags applied to every resource.')
param tags object = {
  application: 'site-inspection-tracker'
}

var suffix = uniqueString(resourceGroup().id)
var appServicePlanName = 'plan-${appName}'
var webAppName = 'app-${appName}-api-${suffix}'
var sqlServerName = 'sql-${appName}-${suffix}'
var sqlDatabaseName = 'sqldb-${appName}'
var staticWebAppName = 'stapp-${appName}-${suffix}'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: appServicePlanSku
  }
  properties: {
    reserved: true // required for Linux plans
  }
}

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// 0.0.0.0 - 0.0.0.0 is the special rule that lets Azure services (the Web App) reach the server.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: sqlDatabaseSkuName
    tier: sqlDatabaseSkuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: staticWebAppLocation
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    // The client is uploaded with the SWA CLI / deployment token, so no repository is linked here.
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Enabled'
  }
}

var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: appServicePlanSku != 'F1'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'Database__ApplyMigrationsOnStartup'
          value: 'true'
        }
        {
          name: 'Database__SeedSampleData'
          value: string(seedSampleData)
        }
        {
          name: 'Swagger__Enabled'
          value: string(enableSwagger)
        }
        {
          name: 'Cors__AllowedOrigins__0'
          value: 'https://${staticWebApp.properties.defaultHostname}'
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: sqlConnectionString
          type: 'SQLAzure'
        }
      ]
    }
  }
  dependsOn: [
    allowAzureServices
  ]
}

@description('Base URL of the API; use it as VITE_API_BASE_URL when building the client.')
output apiUrl string = 'https://${webApp.properties.defaultHostName}'

@description('Public URL of the React client.')
output clientUrl string = 'https://${staticWebApp.properties.defaultHostname}'

output webAppName string = webApp.name
output staticWebAppName string = staticWebApp.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
