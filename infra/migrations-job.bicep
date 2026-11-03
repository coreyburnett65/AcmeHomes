// The migrations job: runs AcmeHomes.MigrationService on demand. The pipeline deploys this with the new image,
// starts an execution, and waits for it to succeed before deploying the API (see ADR 0007).

import { resourceNames, tags } from 'names.bicep'

@description('Full image reference, for example acracmehomesxyz.azurecr.io/acmehomes-migrations:123.')
param image string

param location string = resourceGroup().location

var names = resourceNames(resourceGroup().id)

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: names.containerAppsEnvironment
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: names.containerRegistry
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' existing = {
  name: names.sqlServer
}

resource migrationsIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: names.migrationsIdentity
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: names.apiIdentity
}

// No password: SqlClient gets an Entra token for the user-assigned identity named by its client ID. The connect
// timeout allows for the serverless database resuming from auto-pause.
var connectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${names.sqlDatabase};Authentication=Active Directory Managed Identity;User Id=${migrationsIdentity.properties.clientId};Encrypt=True;Connect Timeout=60'

resource migrationsJob 'Microsoft.App/jobs@2024-03-01' = {
  name: names.migrationsJob
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${migrationsIdentity.id}': {}
    }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      triggerType: 'Manual'
      manualTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
      }
      replicaTimeout: 600
      replicaRetryLimit: 1
      registries: [
        {
          server: registry.properties.loginServer
          identity: migrationsIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'migrations'
          image: image
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__acmehomes'
              value: connectionString
            }
            {
              name: 'DatabaseAccess__ApiIdentityName'
              value: apiIdentity.name
            }
            {
              name: 'DatabaseAccess__ApiIdentityClientId'
              value: apiIdentity.properties.clientId
            }
          ]
        }
      ]
    }
  }
}

output jobName string = migrationsJob.name
