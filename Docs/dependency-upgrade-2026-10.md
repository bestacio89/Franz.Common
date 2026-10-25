# .NET 10 dependency maintenance — October 2026

**Branch:** `chore/platform-quality-and-mediator-roadmap`  
**Pull request:** [#134](https://github.com/bestacio89/Franz.Common/pull/134)  
**Target framework:** `.NET 10`

## Purpose

This maintenance change establishes a supported dependency baseline before the planned Mediator runtime/performance work. It does not change Mediator dispatch behavior or intentionally change public Franz APIs.

## Changes

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
- A provider compatibility warning: `Pomelo.EntityFrameworkCore.MySql` 9.0.0 declares EF Core 9.x compatibility, but this repository resolves EF Core 10. This remains a compatibility decision and must not be hidden with warning suppression.

The GitHub Actions workflow `.github/workflows/dependency-validation.yml` runs restore with NuGet auditing, records outdated and vulnerable direct/transitive package reports, builds Release, and runs the solution tests. A successful merge gate requires all stages to pass and all remaining advisories/compatibility warnings to be reviewed. Merely adding a direct package pin is not proof that every transitive dependency is safe.

## Consumer guidance

- Continue targeting .NET 10.
- Restore from the repository's configured NuGet sources before building.
- Use the latest compatible patch versions for Microsoft.Extensions / ASP.NET Core / EF Core packages; do not independently mix major versions across a dependency graph.
- Re-test optional provider integrations (MariaDB, Oracle, SQL Server, MongoDB/Cosmos DB), messaging transports, and SSO before publishing packages.
- No downstream application repositories were changed by this PR.

## Validation status

The first CI attempt failed during restore, before build or tests:
1. `Grpc >= 2.83.0` could not be resolved because that version is not published for the legacy `Grpc` package.
2. NuGet auditing reported vulnerable transitive dependencies.
3. The Mediator test project referenced a nonexistent source-generator project path.

Corrections now on the branch include the valid gRPC package versions, the corrected source-generator project reference, patched SSH.NET/Snappier dependencies, and removal of invalid cryptography package version references. Do not describe the upgrade as complete until the latest CI run proves restore, audit, Release build, and tests pass.
