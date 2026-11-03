# AI-assisted development log

Notes on where AI assistance (Claude Code) helped, and where its suggestions were changed or rejected.

| Date | Area | What the AI suggested | What I did and why |
|---|---|---|---|
| 2026-11-02 | Solution structure | Three-layer structure (Domain, Infrastructure, Api), minimal APIs, Aspire, Testcontainers, Bicep, Container Apps | Accepted. |
| 2026-11-02 | Data model | Explicit `CommunityFloorPlan` join entity carrying community-specific pricing, an addition to the original model | Accepted. See ADR 0004. |
| 2026-11-02 | Test setup | First generated the test project with the VSTest packages (`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`) | Corrected. xUnit v3 4.x on the .NET 10 SDK requires Microsoft.Testing.Platform, so the build failed. Switched to MTP in `global.json` and removed the VSTest packages. |
| 2026-11-02 | Code analysis | Analyzer rule CA1707 failed the build on underscore-style test names | Turned off CA1707 for `tests/` only in `.editorconfig`, rather than renaming the tests or weakening the rule everywhere. |
