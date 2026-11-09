# gRPC Public API and Release Contract

**Status:** Accepted breaking-change contract for PR #194  
**Date:** 2026-11-09

## 1. Release policy

Franz.Common gRPC 3.0.0 is an intentional breaking modernization. The published 2.3.3 package is historical reference material only; it is **not** an API compatibility baseline.

Do not enable .NET package API validation against 2.3.3, generate or maintain compatibility suppressions for removed 2.x APIs, or retain unsafe/obsolete contracts to satisfy source or binary compatibility. The absence of compatibility with 2.x is an explicit release decision.

The 3.0.0 release must instead prove that its own package surface is coherent, secure, documented, and consumable.

## 2. Package boundaries

### `Franz.Common.Grpc`

The core package references `Grpc.Core.Api` and owns:
- Immutable logical call context and metadata conventions.
- gRPC outcome and status contracts.
- Unary client and server behavior contracts and pipeline providers.
- Explicit server-streaming, client-streaming, and duplex behavior contracts and providers.
- Tenant, validation, authorization, logging, and bounded metrics abstractions.
- Standard .NET ActivitySource and Meter instrumentation.
- Host-independent dependency-injection registrations.

Core does not reference `Grpc.Net.Client`, ASP.NET Core hosting, or the ASP.NET Core adapter. It does not create channels or own handlers.

### `Franz.Common.Grpc.AspNetCore`

The adapter owns ASP.NET Core server interceptor and hosting registration, trusted principal mapping, server-call scope/transport lifecycle, and stream reader/writer bridges (including per-message response write options). The adapter references core in one direction only.

### Client transport

Generated typed clients, channels, handlers, credentials, endpoint configuration, and transport disposal belong to `Grpc.Net.ClientFactory` and the application host. The former Franz channel-owning factory is removed.

The Franz client wrapper governs unary calls only. Generated streaming methods remain directly available through the typed client and do not run through a Franz client-side streaming behavior pipeline. This is an explicit scope boundary.

## 3. Intended 3.0.0 public surface

### Context and outcome
- `GrpcCallContext`: immutable call context, client/server factories, metadata helpers, deadline/cancellation helpers, and conversion to `Metadata`/`CallOptions`.
- `GrpcOutcome`: success/failure factories and gRPC status/exception conversion.

### Server pipeline
- Unary behavior interfaces/providers with explicit order and duplicate-order rejection.
- Server-streaming, client-streaming, and duplex behavior interfaces/providers with deterministic order.
- Validation, tenant resolution, authorization, logging, metrics, and exception-mapping behaviors for supported server RPC shapes.
- Streaming logging and metrics cover complete stream lifetimes without inspecting payloads.
- `GrpcMetricContext` excludes tenant, user, correlation, peer, and request identifiers.
- `FranzGrpcDiagnostics` exposes standard .NET ActivitySource and Meter instrumentation.
- ASP.NET Core interceptor applies pipelines at the transport boundary.

### Client pipeline
- `IGrpcClientBehavior<TRequest,TResponse>` and its provider.
- `FranzGrpcClientBase<TClient>` and `FranzGrpcClient<TClient>` for unary calls using an injected typed client.
- Exception mapping, validation, tenant propagation, logging, and metrics behaviors. Client authorization remains opt-in.
- Client options contain only the implemented unary default timeout.

### Configuration

Only implemented settings remain:
- `FranzGrpcOptions.RequireTenant`.
- `FranzGrpcClientOptions.DefaultClientTimeout`.

Inert timeout, metadata normalization/propagation, payload logging, retry, method-name, and hosting naming-convention switches are removed from the 3.0.0 source contract.

## 4. Explicitly removed 2.x contracts

| Legacy surface | 3.0.0 disposition | Migration |
|---|---|---|
| Caller-controlled identity from `x-user-id` | Removed as an identity source | Derive identity from authenticated host principal |
| `FranzServiceBase.ExecuteUnary` manual pipeline | Removed | Use generated service implementations registered through the ASP.NET Core adapter |
| `IFranzGrpcClientFactory` / `FranzGrpcClientFactory` | Removed; no channel-owning facade | Use `AddGrpcClient<TClient>` and inject generated client |
| Legacy broad `Grpc` dependency | Replaced by `Grpc.Core.Api` in core | Consume the new dependency graph |
| Inert configuration properties | Removed | Remove obsolete keys; configure transport policy through supported host APIs |
| `FranzGrpcHostingOptions.ApplyNamingConventions` | Removed placeholder | Use explicit generated service names and ASP.NET Core conventions |
| Unary-only client wrapper | Retained as unary-only | Use generated typed-client streaming methods directly |
| Metrics accepting full `GrpcCallContext` | Replaced by `GrpcMetricContext` | Use bounded, non-identifying metric dimensions |
| No-op authorization fallback | Replaced by fail-closed deny-all default | Register an explicit application authorization service |

These are intentional breaking changes. Do not add obsolete members, adapters, type-forwarders, package references, or compatibility suppressions to preserve them.

## 5. 3.0.0 release validation

Release validation is forward-looking:

1. Restore, build, and test the full solution with NuGet security auditing.
2. Pack both gRPC packages and inspect package contents and dependency graphs.
3. Verify core has no ASP.NET Core hosting or `Grpc.Net.Client` dependency.
4. Build and run a package-consumer fixture using only the packed 3.0.0 core/adapter pair, a consumer-owned proto, and official typed-client registration.
5. Verify the public API through source review, architecture tests, and the package-consumer fixture. Do not compare it to the 2.3.3 API.
6. Verify migration documentation describes removed APIs and required consumer changes without promising compatibility.
7. Review identity trust, authorization, tenant membership, exception sanitization, cancellation, and streaming lifecycle behavior.

The package-consumer test proves the new contract is usable; it is not a compatibility test against 2.x.

## 6. Host ownership

TLS/mTLS, endpoint configuration, credentials, handlers, channel lifetime, and transport retry policy remain host responsibilities through official gRPC client APIs. Franz must not introduce a parallel channel or certificate-management abstraction. No implicit retry is enabled.
