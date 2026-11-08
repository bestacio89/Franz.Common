# gRPC Public API and Compatibility Inventory

**Status:** Audited at PR #194 checkpoint `ae28d5c749208c244b1ed272addb1df68a07ca8c`  
**Date:** 2026-11-08  
**Purpose:** Establish the compatibility constraints for the production-hardening migration. This inventory is evidence for implementation sequencing, not a promise that every current API will be retained unchanged.

## 1. Published package status

NuGet currently lists `Franz.Common.Grpc` **2.3.3** as its latest published version (last updated 2026-10-28). The package page reports approximately **5.4K total downloads**. The published package is therefore not an unpublished prototype: public API and dependency changes need an explicit migration strategy.

Sources:
- https://www.nuget.org/packages/Franz.Common.Grpc/
- https://www.nuget.org/profiles/BerEstacio89

The repository's current `common.targets` identifies the working source line as 3.0.0. The gRPC hardening PR must not infer that the next package release is free of compatibility obligations merely because the repository is preparing a major-version line. The actual release/versioning decision belongs to the release process and must be made explicitly.

## 2. Public API surface identified

The current source tree exposes the following public contracts and entry points. The implementation phase must compare this list with the compiled public API (including generated XML/API metadata) before removing or changing symbols.

### Context and outcome contracts
- `GrpcCallContext`: public immutable record; client/server factories; `WithTenant`, `WithUser`, deadline/cancellation/header helpers; conversion to gRPC `Metadata` and `CallOptions`.
- `GrpcOutcome`: public outcome record; success/failure factories; exception/RPC conversion; status and `RpcException` conversion.
- Public metadata header constants on `GrpcCallContext`.

### Server pipeline
- `IGrpcServerBehavior<TRequest,TResponse>` and `IGrpcServerBehaviorProvider`.
- `GrpcServerBehaviorProvider`.
- `FranzServiceBase`, including the protected `ExecuteUnary` compatibility entry point.
- Validation, tenant-resolution, authorization, logging, metrics, and exception-mapping behaviour implementations.
- Tenant resolution and authorization/validation/logger/metrics service abstractions.

### Client pipeline and lifecycle
- `IGrpcClientBehavior<TRequest,TResponse>` and `IGrpcClientBehaviourProvider` (note the existing spelling distinction).
- `GrpcClientBehaviorProvider`.
- `IFranzGrpcClientFactory` and `FranzGrpcClientFactory`, currently exposing channel creation and generated-client construction.
- Client behaviour implementations and their supporting contracts.

### Registration and configuration
- `GrpcServiceCollectionExtensions.AddFranzGrpcServer`, `AddFranzGrpcClient`, and `AddFranzGrpcDefaults`.
- `FranzGrpcOptions`, `FranzGrpcClientOptions`, `FranzGrpcClientServiceConfig`, and `FranzGrpcHostingOptions`.
- `GrpcContextExtensions` and `GrpcTenantExecutionContextAccessor`.

This is a source-level inventory. A complete binary API baseline must still be generated from the released 2.3.3 package and compared with the new package outputs before release.

## 3. Confirmed implementation/dependency conflicts

The current core project file references:
- `Grpc` 2.46.6 (legacy package ID).
- `Grpc.Net.Client` 2.84.0 (channel/client implementation).
- Microsoft.Extensions DI and options packages.

The core project therefore violates the accepted ADR dependency boundary. Replace the legacy broad dependency with the narrow, version-aligned `Grpc.Core.Api` contract package where required, and remove `Grpc.Net.Client` from core. Client channel ownership belongs to the official typed-client infrastructure, not this package.

The current `FranzGrpcClientFactory` creates a new `GrpcChannel` and `HttpClientHandler` for each call to `CreateClient`; the factory has no explicit channel disposal/ownership contract. This API cannot remain as a channel-owning abstraction.

The current `GrpcCallContext.FromServerCallContext` treats the caller-controlled `x-user-id` metadata value as the default authenticated `UserId`. This is a trust-boundary defect, not a compatibility guarantee.

The current `AddFranzGrpcDefaults` registers a no-op authorization service. That makes ordinary registration permissive by default and conflicts with the accepted production security policy. Any permissive test/local implementation must be opt-in and explicit.

The current server provider puts exception mapping at the highest numeric order (innermost wrapper), so exceptions thrown by earlier behaviours can bypass it. The ordering contract must be corrected and covered by tests.

## 4. Compatibility disposition

| Surface | Proposed disposition | Reason |
|---|---|---|
| `GrpcCallContext` and metadata constants | Preserve sound context semantics; remove trust in caller identity metadata; add trusted-principal construction at adapter boundary | Core context is useful, but current identity source is unsafe |
| `GrpcOutcome` and status contracts | Preserve public factories where semantics remain valid; sanitize unexpected remote details | Useful stable contract |
| Unary behaviour interfaces/provider | Preserve where possible; make ordering explicit and ensure exception mapping is outermost | Avoid unnecessary source break while fixing execution semantics |
| `FranzServiceBase.ExecuteUnary` | Preserve as a compatibility facade delegating to the canonical pipeline | Existing service implementations may inherit it |
| Streaming contracts | Add explicit new contracts for each RPC shape | New capability; must not overload unary semantics |
| `IFranzGrpcClientFactory` / `FranzGrpcClientFactory` | Retire channel ownership; provide a documented migration to `AddGrpcClient<T>` and a Franz pipeline entry point independent of channel creation | Current design duplicates official lifecycle management and leaks ownership ambiguity |
| Core DI extensions | Keep host-independent registrations in core; move ASP.NET Core hosting/principal integration to adapter | Enforce dependency direction |
| No-op authorization registration | Remove from ordinary production defaults; expose explicit opt-in only if retained | Secure-by-default requirement |
| Inert configuration properties | Implement, remove, or obsolete with a documented window based on actual downstream use | Prevent false configuration promises |

Because 2.3.3 is published, removal of the custom factory and any moved public symbols must not be treated as an incidental refactor. Before release, compare the compiled 2.3.3 public API and decide whether the 3.0.0 release line is the explicit breaking-change vehicle. If a compatibility facade would reintroduce forbidden channel/hosting dependencies, do not retain it merely to preserve a symbol; document the source and binary migration clearly.

## 5. Required compatibility evidence before release

1. Capture the public API of the released 2.3.3 package.
2. Capture the public API of both new package outputs.
3. Produce an explicit diff of removed, moved, added, and obsolete symbols.
4. Build a consumer fixture using the old unary service and context APIs, then migrate it to the new registration and typed-client model.
5. Document the replacement for each retired client-factory method.
6. Verify package dependency graphs: core may depend on `Grpc.Core.Api`, but not `Grpc.Net.Client`, ASP.NET Core hosting, or the adapter package.
7. Include package-consumer validation and migration guidance in the release gate.

## 6. Next implementation slice

The next code checkpoint is the package dependency boundary:
- Add architecture tests that enforce the core/adapter dependency direction.
- Introduce `Franz.Common.Grpc.AspNetCore` with a one-way project/package reference to core.
- Move only types that genuinely require ASP.NET Core hosting or authenticated-principal integration.
- Replace the core gRPC dependency with `Grpc.Core.Api` and remove the channel implementation dependency.
- Keep client channel creation out of the Franz core API; use official typed clients.
- Run focused gRPC tests and the full PR workflow before proceeding to security/pipeline behaviour changes.
