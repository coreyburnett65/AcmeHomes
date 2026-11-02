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

Prerequisites: .NET 10 SDK, Docker Desktop, Node.js 22+.

Instructions will be added once the AppHost exists.

## Architecture decisions

See [docs/decisions](docs/decisions/README.md).
