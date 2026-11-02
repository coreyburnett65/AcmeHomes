# 0002: Minimal APIs instead of controllers

**Status:** Accepted (2026-11-02)

## Context

ASP.NET Core supports both MVC controllers and minimal APIs. The API is mostly read-only catalog endpoints plus a search endpoint.

## Decision

Use minimal APIs organized by feature (one static class per resource that maps a `RouteGroupBuilder`). Endpoints return `TypedResults` so the response types appear accurately in the OpenAPI document. Use the built-in `Microsoft.AspNetCore.OpenApi` package for the document and Scalar for the interactive UI.

## Alternatives considered

- **Controllers:** familiar and still fully supported, but more ceremony. Microsoft's new features and templates focus on minimal APIs.
- **Swashbuckle:** removed from the default templates in .NET 9. The built-in OpenAPI support covers what this project needs.
