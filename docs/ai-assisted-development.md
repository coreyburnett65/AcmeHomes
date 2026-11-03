# AI-assisted development log

Notes on where AI assistance (Claude Code) helped, and where its suggestions were changed or rejected.

| Date | Area | What the AI suggested | What I did and why |
|---|---|---|---|
| 2026-11-02 | Solution structure | Three-layer structure (Domain, Infrastructure, Api), minimal APIs, Aspire, Testcontainers, Bicep, Container Apps | Accepted. |
| 2026-11-02 | Data model | Explicit `CommunityFloorPlan` join entity carrying community-specific pricing, an addition to the original model | Accepted. See ADR 0004. |
| 2026-11-02 | Test setup | First generated the test project with the VSTest packages (`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`) | Corrected. xUnit v3 4.x on the .NET 10 SDK requires Microsoft.Testing.Platform, so the build failed. Switched to MTP in `global.json` and removed the VSTest packages. |
| 2026-11-02 | Code analysis | Analyzer rule CA1707 failed the build on underscore-style test names | Turned off CA1707 for `tests/` only in `.editorconfig`, rather than renaming the tests or weakening the rule everywhere. |
| 2026-11-02 | Dependencies | NuGet audit flagged `System.Security.Cryptography.Pkcs` 9.0.11 (pulled in by `Microsoft.Data.SqlClient`) with two DoS advisories | Pinned the patched 10.0.14 with central transitive pinning instead of upgrading SqlClient past what EF Core was tested with. Warnings-as-errors is what surfaced this. |
| 2026-11-02 | Migrations | The AI checked the migration SQL using `--no-build`, which used a stale assembly and hid a build failure (CA1861 in generated migration code) | Caught by noticing the script was only 9 lines. Marked `Migrations/` as generated code in `.editorconfig`. Lesson: don't skip the build when verifying generated output. |
| 2026-11-02 | Seed data | Original plan was Bogus for generated homes | Changed to a seeded `System.Random`: deterministic output with no extra runtime dependency, and only homes need generating. |
| 2026-11-02 | Testing | Used Testcontainers' parameterless `MsSqlBuilder()` constructor | Obsolete in 4.16, so warnings-as-errors failed the build. Passed the image name explicitly, which also pins the SQL Server version tests run against. |
| 2026-11-02 | API | Floor plan list grouped offerings by entity and sorted after projecting into a response record | Integration test returned 500. The AI first blamed only the `GroupBy`; the real cause was also the `OrderBy` on a constructor-projected DTO, which EF can't translate. Fixed by sorting before projecting. |
| 2026-11-02 | API | Default exception handling | An invalid enum query value (`?status=Demolished`) returned 500 in Development. Added a `StatusCodeSelector` so `BadHttpRequestException` keeps its 400. Caught by a test written for that case. |
| 2026-11-02 | Aspire | Suggested a dedicated MigrationService instead of migrating from the API at startup | Accepted. See ADR 0007. |
| 2026-11-02 | Aspire | Replaced the hand-written `AddDbContextPool` setup with Aspire's `AddSqlServerDbContext` | Accepted. Pooling, retries, health checks, and tracing come from one call. The global single-query setting moved to `.AsSingleQuery()` on the two queries that need it. |
| 2026-11-02 | Deployment | Initial plan had the pipeline run the SQL script that creates database users | Changed before implementation: the migrations job does it from inside Azure, which avoids opening the SQL firewall to build agents. See ADR 0008. |
| 2026-11-02 | Cost | While designing probes, the AI caught that probing `/health` (which includes a database check) would keep serverless SQL from auto-pausing and use up the free offer in days | Probes use `/alive`; `/health` is Development-only. |
| 2026-11-02 | Persistence | `Community.Amenities` as a public `List<string>` | Changed to `IReadOnlyList<string>` with a private setter. A backing-field version tripped IDE0044 because the analyzer can't see EF writing the field. |
| 2026-11-03 | Deployment | Recommended East US 2 and validated the template with `what-if`, which passed | The first pipeline run failed: Azure wasn't accepting new SQL servers in East US 2 for this subscription. `what-if` checks the template, not regional capacity. Checked the SQL capabilities API across regions and moved to Central US. The setup guide now includes that check. |
