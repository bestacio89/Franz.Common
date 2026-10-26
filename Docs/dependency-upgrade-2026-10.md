# .NET 10 dependency maintenance — October 2026

**Branch:** `chore/platform-quality-and-mediator-roadmap`  
**Pull request:** [#134](https://github.com/bestacio89/Franz.Common/pull/134)  
**Target framework:** `.NET 10`  
**Next package version:** `2.3.1`  
**SDK baseline:** `10.0.400`

## Purpose

This maintenance change establishes a supported dependency baseline before the planned Mediator runtime/performance work. It does not change Mediator dispatch behavior or intentionally change public Franz APIs.

## Changes

- Bump the shared Franz.Common package version from 2.3.0 to 2.3.1 for this compatible maintenance release; retain assembly identity 2.3.0.0 and advance file version to 2.3.1.0.
- Align both Azure DevOps pipeline definitions with the repository-pinned .NET SDK 10.0.400. The publishing pipeline now validates produced NuGet package metadata against the intended 2.3.1 release before invoking publisher scripts.
- Upgrade the legacy `Franz.Common.CosmosDB` project from `net7.0` to `net10.0`, update its Microsoft.Extensions dependency, and correct its project-reference path.

- Align direct Microsoft.Extensions, ASP.NET Core, and Entity Framework Core 10 package references to version 10.0.14 where that package version is published. The CI audit is authoritative for the exact resolved graph.
- Replace legacy ASP.NET Core 2.3.11 package references in affected projects with `FrameworkReference Include="Microsoft.AspNetCore.App"`.
- Update the gRPC client stack to the current `Grpc.Net.Client` release. The legacy `Grpc` package remains at its latest published 2.x release because `Grpc` 2.83.0 does not exist on NuGet.
- Update FluentValidation to 12.2.0, SharpCompress to 0.50.7, MongoDB.Driver to 3.12.0, EPPlus to 8.7.2, and SSH.NET to 2026.0.0 in the affected projects.
- Update Microsoft.Azure.Cosmos to 3.63.1 to satisfy the minimum version required by Microsoft.EntityFrameworkCore.Cosmos 10.0.14 and remove the package-downgrade error. Pin System.Security.Cryptography.Pkcs to 10.0.13 where required. The current stable System.Security.Cryptography.Xml package line has no 10.0.13/10.0.14 release; explicit references were removed from projects that consume it through the ASP.NET Core shared framework rather than inventing a nonexistent package version.
- Pin Snappier to a patched release in projects where the dependency graph resolved a vulnerable version.
- Update SSH.NET in affected test projects to the patched 2026 release.
- Fix the Mediator test project's source-generator reference: the source generator lives under `sources/Microsoft.Common.Mediator.SourceGenerator`, not the stale `sources/Franz.Common.Mediator.SourceGenerator` path. The stale reference caused restore to skip a missing project and could prevent generated registrations from being tested.

## Security findings and verification

NuGet auditing previously identified:
- `Snappier` 1.0.0 (high severity; patched versions start at 1.3.1).
- `SSH.NET` 2025.1.0 (high severity; patched version 2026.0.0).
- Vulnerable older `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` versions. Pkcs is pinned to the published 10.0.13 servicing version; XML package references are removed where the API is provided by the ASP.NET Core shared framework. The final audit must confirm no vulnerable transitive XML package remains.
- A restore blocker from the nonexistent `Grpc` 2.83.0 version.
- The MariaDB provider was moved from `Pomelo.EntityFrameworkCore.MySql` 9.0.0 to `Microting.EntityFrameworkCore.MySql` 10.0.13, whose declared dependency range supports EF Core 10. This is a community-maintained fork with renamed package and namespaces; package compatibility is corrected, but behavioral compatibility remains subject to MariaDB integration testing.

The GitHub Actions workflow `.github/workflows/dependency-validation.yml` runs restore with NuGet auditing, builds Release, runs the solution tests with hang diagnostics, and uploads restore-audit/test reports. An optional `dotnet list package` inventory step was removed from the blocking job because it hung after canceled runs; direct/transitive package inventory must be run separately if needed. A successful merge gate requires all stages to pass and all remaining advisories/compatibility warnings to be reviewed. Merely adding a direct package pin is not proof that every transitive dependency is safe.

## Consumer guidance

- Continue targeting .NET 10.
- Restore from the repository's configured NuGet sources before building.
- Use the latest compatible patch versions for Microsoft.Extensions / ASP.NET Core / EF Core packages; do not independently mix major versions across a dependency graph.
- Re-test optional provider integrations (MariaDB, Oracle, SQL Server, MongoDB/Cosmos DB), messaging transports, and SSO before publishing packages.
- No downstream application repositories were changed by this PR.

## Validation status

The latest completed CI run on commit `f359f93195076de588fd8ddd8742d4003e227cfa` passed restore, Release build, and all solution tests. The run reported no NU1901–NU1904 advisories, so the conditional vulnerability-gate step was skipped as designed. The Mediator source-generator integration test now passes after correcting the generator's `ICommandHandler` generic metadata. The subsequent cleanup of duplicate `Snappier` references, redundant Mediator framework package references, release-version changes, SDK alignment, and legacy CosmosDB target changes is newer than that successful run and must be revalidated by CI.

The completed run still reported these warnings:
- `NU1504`: duplicate `Snappier` references in the Bootstrap and HTTP Bootstrap projects. The duplicate entries have since been removed; confirm in the next run.
- `NU1510`: explicit Hosting/Logging abstractions references in Mediator are supplied by the shared framework. Those redundant package references have since been removed; confirm in the next run.
- The prior `NU1608` provider mismatch was addressed by replacing Pomelo 9.0.0 with `Microting.EntityFrameworkCore.MySql` 10.0.13. Confirm the next CI restore has no provider-version warning, and run MariaDB 11.4 LTS integration tests before treating the provider switch as behaviorally validated.

The first CI attempt failed during restore, before build or tests:
1. `Grpc >= 2.83.0` could not be resolved because that version is not published for the legacy `Grpc` package.
2. NuGet auditing reported vulnerable transitive dependencies.
3. The Mediator test project referenced a nonexistent source-generator project path.

Corrections now on the branch include the valid gRPC package versions, the corrected source-generator project reference, patched SSH.NET/Snappier dependencies, and removal of invalid cryptography package version references. Do not describe the upgrade as complete until the latest CI run proves restore, audit, Release build, and tests pass.

## MariaDB provider qualification — follow-up work

PR #134 is merged with `Microting.EntityFrameworkCore.MySql` 10.0.13 selected as the EF Core 10 candidate. The provider switch is not considered behaviorally qualified by package compatibility or a successful solution build alone.

The follow-up qualification branch adds a real-database integration test using Testcontainers and MariaDB 11.4. The initial test exercises:

- Container startup and provider connectivity.
- Schema creation through EF Core.
- Generated primary keys and persistence of a parent/child relationship.
- SQL translation for string filtering, related-entity predicates, aggregate counts, and decimal comparisons.
- Relationship loading.
- Transaction rollback and verification that rolled-back data is absent.

**Current status:** on commit `c8c25f9e500c965d610759d3eff30c7c15c511b5`, the Release build succeeded and the integration-test project reported 14 passed and 0 failed, including the new MariaDB qualification test. The overall solution test step still failed because three Azure Cosmos DB emulator tests could not start their container; the emulator process crashed during startup with a Service Fabric `0x800700B7` exception. That failure is outside the MariaDB test path. A follow-up commit replaces the obsolete parameterless `MariaDbBuilder` constructor with the explicit image constructor; the full CI run for the latest PR head remains to be confirmed.

This first slice does not establish migration compatibility, MySQL 8.4 compatibility, parity with the previous Pomelo provider, or correct behavior of Franz.Common's `AddMariaDatabase<TDbContext>` composition path. Those remain explicit follow-up gates. Next, run the new test on the exact PR head; then add migration and service-registration coverage based on the results, and evaluate MySQL 8.4 separately if MySQL is a supported target.

