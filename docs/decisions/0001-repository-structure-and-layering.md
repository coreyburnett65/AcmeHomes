# 0001: Repository structure and three-layer architecture

**Status:** Accepted (2026-11-02)

## Context

The project has a .NET back end, a Next.js front end, infrastructure templates, and later a search component and a worker service. It needs a structure that is easy to navigate in an interview and doesn't add ceremony the domain doesn't need.

## Decision

- A single repository (monorepo) with `src/`, `tests/`, `web/`, `infra/`, and `docs/`.
- `.slnx` solution format, `global.json` pinning the SDK, `Directory.Build.props` for shared settings, and central package management in `Directory.Packages.props`.
- Three .NET layers, with dependencies pointing inward:
  - **Domain:** entities and enums, no package dependencies.
  - **Infrastructure:** EF Core `DbContext`, entity configurations, migrations, seeding.
  - **Api:** HTTP endpoints, DTOs, OpenAPI.
- No repository pattern over EF Core. `DbContext` already provides unit of work and repository semantics.
- New projects (`Search`, `InventoryWorker`) are added in the phase that needs them, not up front.

## Alternatives considered

- **Full Clean Architecture** (separate Application layer, MediatR, CQRS): widely recognized, but mostly empty layers and indirection for a read-heavy catalog domain.
- **Single project:** simplest, but makes it harder to share Domain and Infrastructure with the worker service later.
- **Separate repositories for front end and back end:** cleaner ownership boundaries, but more overhead for a one-person project and harder to show as one piece of work.
