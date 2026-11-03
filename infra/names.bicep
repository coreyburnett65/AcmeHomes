// Resource names shared by every template, so later deployments can find resources created by main.bicep
// without passing outputs between pipeline stages. Names that must be globally unique get a suffix derived
// from the resource group ID, which is stable for the life of the group.

@export()
func resourceNames(resourceGroupId string) object => {
  logAnalytics: 'log-acmehomes'
  containerAppsEnvironment: 'cae-acmehomes'
  containerRegistry: 'acracmehomes${uniqueString(resourceGroupId)}'
  sqlServer: 'sql-acmehomes-${uniqueString(resourceGroupId)}'
  sqlDatabase: 'acmehomes'
  apiIdentity: 'id-acmehomes-api'
  migrationsIdentity: 'id-acmehomes-migrations'
  apiApp: 'ca-acmehomes-api'
  migrationsJob: 'caj-acmehomes-migrations'
}

@export()
var tags = {
  application: 'acmehomes'
  environment: 'demo'
}
