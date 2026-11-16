using './main.bicep'

param appName = 'siteinspect'
param appServicePlanSku = 'B1'
param sqlDatabaseSkuName = 'Basic'
param sqlDatabaseSkuTier = 'Basic'
param seedSampleData = true
param enableSwagger = true

// Read from the environment so the password never lands in source control:
//   export SQL_ADMIN_PASSWORD='<a strong password>'
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
