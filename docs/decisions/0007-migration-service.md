# 0007: Database migrations run in a dedicated migration service

**Status:** Accepted (2026-11-02)

## Context

EF Core migrations and seed data must be applied before the API serves requests, both locally and in Azure. The API is read-only and shouldn't need permission to change the schema.

## Decision

A small worker project, `AcmeHomes.MigrationService`, applies pending migrations (which also runs the seeder, see the seeding code in Infrastructure) and then exits. It is the only service that registers the seeder.

- **Locally:** the Aspire AppHost starts it after SQL Server is healthy, and starts the API with `WaitForCompletion`, so the API only runs after migrations succeed.
- **In Azure:** the same container image can run as a Container Apps Job during deployment, before the new API revision takes traffic.

A failed migration sets a non-zero exit code, which stops dependent resources locally and fails the job in Azure.

## Alternatives considered

- **API applies migrations at startup:** simplest, but every replica races to migrate (EF Core 9+ takes a lock, so it's safe, just wasteful), startup gets slower, and the API's database identity needs schema-change permissions.
- **SQL script or EF migration bundle run from the pipeline:** a good option for production, but it needs the pipeline agent to reach the database and doesn't help local development. The migration service covers both.
