// Shared infrastructure: logging, the Container Apps environment, the container registry, managed identities,
// and Azure SQL. The migrations job and the API are deployed separately (migrations-job.bicep, api.bicep) so the
// pipeline can run migrations before the new API version goes live. See docs/deployment.md and ADRs 0005 and 0008.

import { resourceNames, tags } from 'names.bicep'

@description('Azure region for all resources. Defaults to the resource group\'s region.')
param location string = resourceGroup().location

var names = resourceNames(resourceGroup().id)

// Built-in role that lets an identity pull images from a container registry.
var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: names.logAnalytics
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      // A hard stop on runaway logging costs. Demo traffic stays far below this.
      dailyQuotaGb: 1
    }
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: names.containerAppsEnvironment
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    // Consumption only: apps scale to zero and are billed per second of use, within a monthly free allowance.
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: names.containerRegistry
  location: location
  tags: tags
  sku: {
    // About $5/month: the only fixed cost in the demo.
    name: 'Basic'
  }
  properties: {
    // Apps pull with managed identity, so the shared admin account stays off.
    adminUserEnabled: false
  }
}

// Separate identities give least privilege: the API can only read data, the migrations job owns the schema.
resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: names.apiIdentity
  location: location
  tags: tags
}

resource migrationsIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: names.migrationsIdentity
  location: location
  tags: tags
}

resource apiAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, apiIdentity.id, acrPullRoleId)
  scope: registry
  properties: {
    roleDefinitionId: acrPullRoleId
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource migrationsAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, migrationsIdentity.id, acrPullRoleId)
  scope: registry
  properties: {
    roleDefinitionId: acrPullRoleId
    principalId: migrationsIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: names.sqlServer
  location: location
  tags: tags
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      // Microsoft Entra authentication only: no SQL logins or passwords exist. The migrations identity is the
      // server admin so it can apply migrations and create the API's read-only user (see ADR 0008).
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: migrationsIdentity.name
      sid: migrationsIdentity.properties.principalId
      principalType: 'Application'
      tenantId: subscription().tenantId
    }
  }
}

// Container Apps on the consumption plan don't have fixed outbound IPs, so the server accepts connections from
// Azure services. Every connection still has to authenticate with Entra. A private endpoint would close this
// completely but adds VNet costs the demo doesn't need.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: names.sqlDatabase
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    // The Azure SQL free offer: 100,000 vCore-seconds and 32 GB each month. When the allowance runs out the
    // database pauses until the next month instead of billing.
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    autoPauseDelay: 60
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    requestedBackupStorageRedundancy: 'Local'
  }
}

output registryLoginServer string = registry.properties.loginServer
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
