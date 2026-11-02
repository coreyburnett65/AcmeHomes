# 0003: .NET Aspire for local orchestration

**Status:** Accepted (2026-11-02)

## Context

Running the app locally will eventually need SQL Server, the API, the Next.js front end, a Service Bus emulator, and a worker service. Starting and wiring these by hand is error-prone and hard to explain to anyone cloning the repo.

## Decision

Use .NET Aspire:

- **AppHost** declares the resources (a SQL Server container, the API, and later the Service Bus emulator, the worker, and the Next.js app) and injects connection strings into each service.
- **ServiceDefaults** adds OpenTelemetry, health checks, service discovery, and HTTP resilience to every .NET service.

Running the AppHost starts everything and opens the Aspire dashboard with logs, traces, and metrics.

## Consequences

- Docker Desktop is required for local development.
- Aspire is used for local development. Deployment goes through Azure DevOps and Bicep (see 0005) so that the pipeline is explicit and explainable.

## Alternatives considered

- **Docker Compose:** works, but has no built-in service wiring or telemetry dashboard, and needs a separate configuration style.
- **LocalDB plus manual startup:** fewest moving parts at first, but gets worse with each service added.
