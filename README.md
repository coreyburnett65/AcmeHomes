# Acme Homes

An AI-powered home search site for a fictional homebuilder. Visitors search in plain English ("four bedrooms under $450K with a first-floor primary suite near Frederick"), and the app turns the query into validated, structured filters and returns matching move-in-ready homes and buildable floor plans.

> All builder names, communities, and data are fictional.

## Status

Work in progress. Currently building the solution structure, data model, and Web API.

## Tech stack

- .NET 10, ASP.NET Core minimal APIs, Entity Framework Core, Azure SQL
- .NET Aspire for local orchestration and observability
- Next.js with TypeScript (headless front end)
- Azure AI Search and the Claude API for natural-language search
- Azure Service Bus with a .NET worker for inventory sync
- Azure DevOps Pipelines deploying to Azure Container Apps

## Repository layout

| Path | Purpose |
|---|---|
| `src/` | .NET projects (Domain, Infrastructure, Api, Aspire AppHost and ServiceDefaults) |
| `tests/` | xUnit unit and integration tests |
| `web/` | Next.js front end |
| `infra/` | Bicep templates for Azure resources |
| `docs/decisions/` | Architecture decision records |

## Running locally

Prerequisites: .NET 10 SDK, Docker Desktop (running), Node.js 22+.

```
dotnet tool restore
dotnet run --project src/AcmeHomes.AppHost
```

The Aspire AppHost starts everything in order:

1. A SQL Server container (kept running between sessions, with its data in a Docker volume)
2. The **migrations** service, which applies EF Core migrations, seeds the demo data, and exits
3. The **API**, once migrations have completed

The Aspire dashboard opens in your browser with logs, traces, and metrics for every service. The API reference (Scalar) is at `/scalar` on the API's URL, which the dashboard links to.

To reset the local database, stop the AppHost and remove the SQL Server container and its volume in Docker Desktop.

## Running tests

```
dotnet test --solution AcmeHomes.slnx
```

Infrastructure and API tests start their own SQL Server container through Testcontainers, so Docker must be running. The first run downloads the SQL Server image.

## Architecture decisions

See [docs/decisions](docs/decisions/README.md).
