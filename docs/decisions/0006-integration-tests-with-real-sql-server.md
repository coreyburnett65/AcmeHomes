# 0006: Integration tests against real SQL Server

**Status:** Accepted (2026-11-02)

## Context

API integration tests need a database. EF Core's InMemory provider is fast but doesn't enforce relational constraints and evaluates queries differently from SQL Server, so tests can pass while production queries fail.

## Decision

Integration tests use `WebApplicationFactory` with a SQL Server container started by Testcontainers. The container is shared across a test collection and the schema is created from the real migrations.

## Consequences

- Docker is required to run integration tests, locally and on the CI agent (Microsoft-hosted Ubuntu agents include it).
- Tests are slower than in-memory tests, so pure logic is covered by plain unit tests in `AcmeHomes.Domain.Tests`.

## Alternatives considered

- **EF Core InMemory provider:** Microsoft recommends against it for testing.
- **SQLite in-memory:** relational, but a different SQL dialect from production.
- **Shared LocalDB instance:** Windows-only and harder to run in CI.
