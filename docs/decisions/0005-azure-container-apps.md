# 0005: Azure Container Apps as the hosting target

**Status:** Accepted (2026-11-02)

## Context

The API, the worker service, and the Next.js front end need hosting. Costs must stay low because the app is only used for demos.

## Decision

Deploy each service as a container to Azure Container Apps on the consumption plan. Resources are defined in Bicep under `infra/` and deployed by Azure DevOps Pipelines. Container images are stored in Azure Container Registry (Basic tier).

## Consequences

- Apps can scale to zero between demos, so there is little or no compute cost when idle. The first request after idle has a cold-start delay.
- The worker can scale on Service Bus queue length (KEDA) later.
- Container Registry Basic has a small fixed monthly cost. Azure SQL serverless with auto-pause keeps database cost near zero when idle.

## Alternatives considered

- **App Service:** mature and simple, but the lowest always-on tiers cost money even when idle, and running a background worker there is awkward.
- **Azure Kubernetes Service:** far more operational overhead than this project needs.
