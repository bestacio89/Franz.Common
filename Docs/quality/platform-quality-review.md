# Platform Quality Review — develop

**Review baseline:** `develop` at `287364ba37269ea23bcfb54343287c5d87e83a26` (2026-10-25 review).  
**Scope:** static review of repository metadata, project files, CI configuration, and the Mediator dispatch/registration implementation. This is not a successful build or test report: the execution environment could not clone the repository or run its .NET toolchain against the checkout.

## Confirmed findings

### P1 — Generated registration is not strictly generated-only
`GeneratedHandlerRegistrationProvider` first looks for a conventional generated type, then scans the target assembly, then scans all loaded assemblies, and finally silently falls back to reflection registration while swallowing exceptions. This conflicts with the stated deterministic / Native AOT-first V2 contract, introduces unpredictable startup work, can select an unrelated provider, and hides registration failures. The fallback behavior needs an explicit compatibility policy rather than an undocumented catch-all.

### P1 — Mediator invoker cache key does not encode the response contract
`FranzDispatcher.ResponseHandlerCache` is keyed only by the concrete request type, while the cached factory captures `typeof(TResponse)`. The same request type can be dispatched through more than one generic response contract, so the cache key should represent both request and response types (or dispatch registration should validate and enforce a single response contract). Add a regression test before changing this behavior.

### P1 — Dependency update automation is disabled
`.github/dependabot.yaml` contains only commented configuration. The repository has many independently versioned project files, making manual package maintenance error-prone. Enable grouped NuGet and GitHub Actions updates, with CI as the merge gate.

### P2 — ASP.NET Core package age deserves compatibility review
`Franz.Common.Mediator.csproj` references `Microsoft.AspNetCore.Http` 2.3.11 alongside .NET 10 and Microsoft.Extensions 10.0.10 packages. Determine which APIs are actually required and prefer the .NET 10 shared framework / aligned abstractions when valid. Do not remove the reference blindly: this must be verified with restore, build, package dependency inspection, and consumer compatibility tests.

### P2 — Source generator target and diagnostics need explicit validation
The mediator source generator currently targets `net10.0`. Confirm the intended compiler/SDK compatibility matrix and analyzer loading requirements. A generator target change is a packaging and compatibility decision, not a cosmetic cleanup. Generator diagnostics should report unsupported handler shapes and duplicate registrations at compile time rather than silently deferring failures to runtime.

### P2 — Static review indicates dispatch hot-path opportunities
The dispatcher uses runtime-generated invokers and caches, but each send path also performs request-context resets, observer notifications, pipeline service resolution, and dynamic pre/post-processor dispatch. Optimizations must be measured end-to-end with realistic pipelines enabled; isolated handler invocation benchmarks alone would overstate application-level gains.

### P2 — CI needs a verified, single source of truth
The repository contains multiple Azure pipeline definitions and a custom solution file. Before changing CI behavior, verify which definitions are active, ensure the Mediator tests run against the same built outputs as the rest of the suite, and publish test/coverage results. A repository-wide warning-free claim requires an actual Release build with analyzers enabled.

## Quality gate for maintenance work

1. Inventory all direct and transitive NuGet dependencies and identify outdated, deprecated, vulnerable, and duplicate references.
2. Update compatible patch/minor versions in coherent groups; treat major versions and provider SDK upgrades as separate compatibility changes.
3. Enable nullable/analyzer rules consistently, fix actionable warnings at source, and document any justified suppressions with scope and rationale.
4. Run restore, Release build, unit tests, architecture tests, integration tests, package validation, and Native AOT/trimming checks where supported.
5. Verify packed package dependency graphs and compile representative downstream consumers before merging.
6. Report before/after warning counts, test results, package changes, and any remaining blockers. No warning count should be described as cleared without build evidence.

## Execution boundary

This review and plan do not authorize breaking public API changes or modifications to downstream repositories. Any required consumer code changes, package contract changes, or behavioral changes will be proposed separately for approval.
