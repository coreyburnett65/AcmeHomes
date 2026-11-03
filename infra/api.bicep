// The API container app. Deployed after the migrations job succeeds, so the new version never runs against an
// out-of-date schema.

import { resourceNames, tags } from 'names.bicep'

@description('Full image reference, for example acracmehomesxyz.azurecr.io/acmehomes-api:123.')
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

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: names.apiIdentity
}

// No password: the API connects as its own identity, which the migrations job granted db_datareader only.
var connectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${names.sqlDatabase};Authentication=Active Directory Managed Identity;User Id=${apiIdentity.properties.clientId};Encrypt=True;Connect Timeout=60'

// .NET container images listen on 8080 by default.
var port = 8080

// Probes use /alive, which has no database check, so probing never keeps serverless SQL awake (see Extensions.cs).
var aliveProbe = {
  path: '/alive'
  port: port
}

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: names.apiApp
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        // TLS terminates here; the app itself serves plain HTTP inside the environment.
        external: true
        targetPort: port
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: registry.properties.loginServer
          identity: apiIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
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
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: aliveProbe
              periodSeconds: 2
              failureThreshold: 30
            }
            {
              type: 'Readiness'
              httpGet: aliveProbe
              periodSeconds: 10
            }
            {
              type: 'Liveness'
              httpGet: aliveProbe
              periodSeconds: 30
            }
          ]
        }
      ]
      scale: {
        // Scale to zero between demos. The first request after idle waits for a cold start.
        minReplicas: 0
        maxReplicas: 1
        rules: [
          {
            name: 'http'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

output url string = 'https://${api.properties.configuration.ingress.fqdn}'
