# gRPC Architecture Decisions

**Status:** Accepted for implementation in PR #194  
**Decision date:** 2026-11-08  
**Scope:** Package boundaries, execution contracts, security, lifecycle, configuration, and compatibility for Franz.Common gRPC.

This document records the architecture decisions approved before production implementation. It is normative for the remaining phases of PR #194. Any deviation must be justified in a later decision record and reflected in tests and package documentation.

## 1. Package boundaries

### 1.1 `Franz.Common.Grpc`

This package owns the gRPC protocol-level abstractions and Franz execution model:

- Immutable logical call context and metadata conventions.
- gRPC outcome/status contracts and error metadata.
- Unary and streaming behaviour contracts.
- Behaviour pipeline composition and ordering contracts.
- Client-side pipeline abstractions that do not create or own channels.
- Dependency-injection registrations that do not require an ASP.NET Core host.

The package is **hosting-runtime independent**, not transport independent: gRPC protocol contracts may use the supported `Grpc.Core.Api` surface where required, but the package must not depend on ASP.NET Core hosting or `Grpc.Net.Client` channel implementations.

The legacy `Grpc` package reference must be evaluated for replacement by the narrow, version-aligned `Grpc.Core.Api` package. Do not retain the legacy package merely for transitive convenience.

### 1.2 `Franz.Common.Grpc.AspNetCore`

This adapter owns ASP.NET Core-specific integration:

- Server interceptor and endpoint/hosting integration.
- Conversion from ASP.NET Core authenticated `ClaimsPrincipal` to trusted Franz identity.
- ASP.NET Core DI and hosting extensions.
- Server-call lifecycle integration and tenant execution-context scoping.

The adapter references the core package. The core package must not reference this adapter or ASP.NET Core assemblies.

### 1.3 Client channel and typed-client ownership

Use the official `Grpc.Net.ClientFactory` integration and typed-client registration (`AddGrpcClient<TClient>`) for client channel and handler lifecycle.

Franz must not create a new `GrpcChannel` for each client request, nor dispose a channel it does not own. The existing `IFranzGrpcClientFactory` / `FranzGrpcClientFactory` is not retained as a parallel channel factory. Its useful Franz-specific pipeline functionality must be preserved independently of channel construction and applied around typed-client calls.

The host owns endpoint configuration, credentials/handlers, channel lifetime, and typed-client registration. Franz owns its logical call context and behaviour policy. If later implementation proves that an additional Franz client integration package is unavoidable, record that as a separate package-boundary decision before introducing it.

## 2. Execution contracts

### 2.1 Unary compatibility

Do not preserve `FranzServiceBase.ExecuteUnary` merely for source compatibility. The canonical server pipeline must be applied by the hosting adapter at the transport boundary, so service implementations cannot bypass it accidentally. Remove the base-class/manual-pipeline contract if it prevents that invariant; provide a migration path to ordinary generated gRPC service implementations registered through the adapter.

Do not make application service methods responsible for manually invoking behaviours. The hosting adapter must ensure the pipeline cannot be accidentally bypassed by normal service registration.

### 2.2 Streaming

Streaming is not a unary request/response with a different transport wrapper. Define explicit contracts and lifecycle semantics for all four RPC shapes:

| RPC shape | Request side | Response side |
|---|---|---|
| Unary | One request | One response |
| Server streaming | One request | Asynchronous response stream |
| Client streaming | Asynchronous request stream | One response |
| Duplex streaming | Asynchronous request stream | Asynchronous response stream |

The Franz-facing streaming contracts should use asynchronous stream abstractions and cancellation tokens. Transport-specific readers/writers remain at the adapter boundary. The full call lifetime—including enumeration, writes, cancellation, exceptions, and disposal—must remain inside the relevant pipeline scope.

A behaviour must either invoke its continuation exactly once or deliberately short-circuit. Streaming implementations must additionally define how early termination, partial consumption, and disposal propagate.

## 3. Identity, authentication, authorization, and tenancy

- Never derive authenticated `UserId` from caller-controlled `x-user-id` metadata.
- Server identity is derived only from the authenticated principal supplied by the hosting environment. Missing authentication remains unauthenticated; it is not silently replaced by metadata.
- `x-user-id` is not an identity authority. If retained for compatibility or diagnostics, it must be treated as untrusted input and must not override the principal.
- Authentication is a host responsibility. Franz authorization evaluates the trusted identity and explicit policy.
- No implicit allow-all authorization service is registered by production defaults. A permissive no-op implementation, if retained for tests or deliberate local scenarios, must be explicit and must not be selected by ordinary production registration.
- Tenant identifiers from metadata are selectors, not proof of membership. Resolve tenant context server-side and independently authorize the principal's membership.
- `RequireTenant` remains an explicit opt-in policy; it is not globally forced on services that do not require tenancy.
- Tenant execution context must be scoped to the complete RPC lifetime and cleared on success, failure, cancellation, and stream disposal.

## 4. Pipeline and exception semantics

- Every unary server behavior declares an explicit numeric execution order. Lower values execute earlier; exception mapping uses the reserved `int.MinValue` position. Built-in behaviors use documented reserved positions, and duplicate positions for the same RPC contract fail pipeline construction rather than falling back to DI registration order.
- Custom unary behavior authors must choose an explicit order. The streaming contracts will use the same ordering model when their DI providers are introduced.
- Pipeline order is explicit, deterministic, and documented.
- Exception mapping is the outermost boundary so it observes failures from validation, tenant resolution, authorization, logging, metrics, and the handler.
- Custom behaviour ordering must be an explicit contract. Registration order alone is not an adequate undocumented extension point.
- Cancellation is not an internal server failure. Map cancellation according to whether the server call token was cancelled and preserve the distinction between caller cancellation and unrelated operation cancellation.
- Unexpected exceptions must not expose their original message, stack, or sensitive details in remote status details. Detailed exceptions remain available only to trusted local diagnostics.
- Known Franz/platform failures map to deliberate status codes; unknown failures map to a sanitized internal error.
- Correlation identifiers may be propagated, but user and tenant identity metadata must never be treated as trusted authentication evidence.

## 5. Client lifecycle and transport policy

- Reuse official typed-client/channel infrastructure.
- Do not create or dispose channels per RPC.
- Deadlines and cancellation are propagated explicitly and tested.
- TLS and mTLS configuration is supplied through standard gRPC client/HTTP handler configuration; Franz must not invent a competing certificate-management abstraction without a demonstrated requirement.
- Metadata propagation uses an allowlisted, documented policy. Credentials and sensitive metadata must not be logged.
- No automatic retry is enabled by default. Retries require an explicit policy and must be restricted to operations whose idempotency and retry semantics are known. Streaming calls are not implicitly retryable.

## 6. Configuration policy

Every existing option must receive one of three dispositions before the configuration phase closes:

1. Implement it with observable, tested behaviour and startup validation.
2. Remove it before the package is released if it has no justified contract.
3. Deprecate it with a documented compatibility window if already-consumed public API compatibility requires that treatment.

Inert options must not remain advertised as working controls. Configuration defaults must be safe, and invalid combinations must fail during startup rather than during an arbitrary RPC.

## 7. Compatibility and release policy

- Preserve only contracts whose semantics remain correct and whose dependencies respect the accepted architecture. Source or binary compatibility is not a reason to retain an unsafe contract.
- Introduce explicit streaming APIs; do not overload unary abstractions or silently change their meaning.
- Remove the custom channel-owning factory in favour of official typed-client lifecycle management. Preserve genuinely useful pipeline functionality independently, with an explicit migration path.
- The user has explicitly authorized full gRPC modernization, including removal of legacy/deprecated APIs and unsafe contracts. The published 2.3.3 package history must be documented, but does not impose a requirement to retain defective APIs. Treat the 3.0.0 release line as the intended breaking-change vehicle and provide migration guidance.
- Package names, dependency direction, public APIs, and migration guidance must be covered by architecture tests and package-consumer tests.

## 8. Phase gates

Phase 1 is complete when the implementation plan and tests reflect these decisions, including:

- Core/adapter dependency-direction tests.
- A trusted-principal integration test proving forged `x-user-id` cannot set identity.
- Explicit anonymous/authenticated/authorized/tenant-required policy tests.
- Ordering tests proving exception mapping encloses every behaviour.
- Contract-level tests for each streaming shape and its cancellation/disposal lifecycle.
- Typed-client registration and channel-lifecycle tests using the official factory.
- A compatibility inventory for existing public APIs and published package versions.

No production refactor is considered complete solely because the solution builds. Each behaviour change requires a regression test and successful PR CI on the exact head.
