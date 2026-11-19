# Versioning Policy — Franz.Common

Franz.Common uses semantic versioning (SemVer) for its published NuGet packages. This policy separates **framework package versions**, **.NET target/SDK versions**, and **third-party dependency versions**; they are different concerns and must not be conflated.

## Version sources

| Concern | Authoritative source | Rule |
|---|---|---|
| NuGet package version | `common.targets` | All packages importing this file inherit the same release version unless an explicit project-level exception is documented. |
| .NET target framework | `common.targets` and project-specific target settings | Supported library projects target `.NET 10` (`net10.0`). Any multi-target or legacy exception must be intentional and documented. |
| .NET SDK | `global.json` | CI and local builds use the pinned SDK feature band. Azure DevOps must install the same SDK version. |
| Third-party NuGet dependencies | `PackageReference` versions in project files | Upgrade to a tested compatible version; do not assume all packages share a version number. A future central package file must preserve compatibility exceptions. |

## Release version

The coordinated release target is **3.0.1**, a compatible patch release under SemVer. The shared `common.targets` file sets package version **3.0.1**, assembly version **3.0.0.0**, and file version **3.0.1.0**. Keeping the assembly version stable avoids an unnecessary binary identity change for a patch release.

`PackageValidationBaselineVersion` is **3.0.0**, the last published stable release. Compatibility validation for this patch must compare against that published package. Do not move the baseline beyond the latest published version merely to make validation pass; intentional public API changes must be reviewed and classified according to SemVer.

## Semantic versioning

- **MAJOR** — public API or behavior changes that are incompatible with consumers.
- **MINOR** — backward-compatible public capabilities or modules.
- **PATCH** — compatible fixes, dependency maintenance, and security updates that do not intentionally change the public API.

## Release gates

Before publishing packages:

1. Restore with NuGet auditing enabled and review every advisory.
2. Build Release and run the complete solution test suite.
3. Validate every produced package's identity and version metadata.
4. Verify that optional infrastructure providers remain compatible with the selected EF Core version.
5. Publish only from the approved release branch and only after the validation pipeline passes.

A direct dependency pin is not proof that the resolved transitive graph is safe. Do not suppress provider compatibility warnings to make a build appear clean.
