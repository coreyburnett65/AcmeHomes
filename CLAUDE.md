# CLAUDE.md

## About this project

This is a portfolio project: an AI-powered home search site for a **fictional homebuilder**. Visitors can search in plain English (for example, "four bedrooms under $450K with a first-floor primary suite near Frederick") and the app turns that into structured filters and returns matching homes and floor plans.

The goal is to demonstrate modern, production-quality skills to employers. The developer is a senior .NET engineer and architect with 20+ years of experience (much of it in Sitecore and enterprise integrations) who is updating his skills in cloud, headless front ends, and AI. The finished project will be shown in job interviews, so code quality, clear architecture, and explainable decisions matter more than feature count.

**Deadline:** core app complete by end of December 2026; polish and documentation in early January 2027.

## How I want to work with you

- Explain your reasoning for architectural choices and non-obvious code. I need to understand and defend every part of this codebase in interviews.
- Propose a plan before large changes, and keep changes small and reviewable.
- Prefer clear, idiomatic code over clever code. Follow current best practices for each technology.
- Point out tradeoffs and alternatives when a decision is significant.
- Write tests alongside features, not afterward.
- Never commit secrets. Use user secrets locally and Azure Key Vault or pipeline variables in the cloud.
- Keep Azure costs low: prefer free or lowest tiers, and remind me which resources can be shut down between demos.

## Tech stack

- **Back end:** .NET 10, ASP.NET Core Web API, C#
- **Data:** Azure SQL (SQL Server locally), Entity Framework Core with migrations
- **Front end:** Next.js with TypeScript, consuming the API headlessly
- **Search:** Azure AI Search
- **AI:** Claude API for translating natural-language queries into structured search filters (model name kept in configuration)
- **Messaging:** Azure Service Bus with a .NET worker service
- **CI/CD:** Azure DevOps Pipelines, deploying to Azure Container Apps or App Service
- **Testing:** xUnit for .NET; a standard test setup for Next.js

## Architecture overview

1. **Web API** serves markets, communities, floor plans, homes, and search results.
2. **Next.js front end** renders community pages, home and plan detail pages, filters, and the natural-language search box.
3. **Search layer:** homes and plans are indexed in Azure AI Search. The API queries the index with structured filters.
4. **Natural-language search:** the API sends the user's query to Claude with a strict schema, receives JSON filters, **validates them server-side**, and only then runs the search. Invalid, vague, or impossible queries (for example, "a mansion for $50K") must be handled gracefully with helpful feedback. AI output is never trusted blindly.
5. **Inventory sync worker:** a mock external system publishes inventory updates (status, price, move-in date) to Service Bus; a worker service consumes them, updates the database, and refreshes the search index. Handle retries, idempotency, and dead-lettering.

## Data model

- **Market:** a metro area (e.g., Frederick, Baltimore).
- **Community:** belongs to a market; location (lat/long), school district, amenities, HOA details.
- **FloorPlan:** offered in one or more communities; bedrooms, bathrooms, square footage, stories, primary-suite location, base price.
- **Home:** actual inventory; a floor plan built on a specific lot in a community, with status (Available, UnderConstruction, Sold), price, and estimated move-in date.
- **Feature:** tags attached to plans or homes (finished basement, three-car garage, first-floor primary suite, etc.).

**Key design point:** a search can match either a move-in-ready **Home** or a buildable **FloorPlan** in a community. Results should clearly distinguish the two. This plan-versus-home distinction is a deliberate talking point.

All data is fake but realistic. Do not use real builder names, branding, or data.

## Roadmap

1. **Weeks 1–2 (early to mid November):** solution structure, data model, EF Core migrations, seed data, Web API, Azure DevOps pipeline, first deployment to Azure.
2. **Weeks 3–4 (late November):** Next.js front end with community, plan, and home pages and standard filters (no AI yet).
3. **Weeks 5–7 (December):** Azure AI Search index, natural-language search with Claude, validation and edge-case handling.
4. **Week 8 (late December):** Service Bus inventory sync worker, test coverage, performance cleanup.
5. **January:** README with architecture diagram, two-minute demo video, final polish.

**Current phase:** Weeks 1–2. Start with the solution structure and data model.

## Documentation expectations

- Keep the README current: what the app does, architecture diagram, how to run locally, how it deploys.
- Record significant decisions briefly in `docs/decisions/` (one short markdown file per decision: context, decision, alternatives considered).
- Note where AI-assisted development helped and where its suggestions were changed or rejected, so I can speak to this in interviews.
