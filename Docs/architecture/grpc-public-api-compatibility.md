# gRPC Public API and Compatibility Inventory

**Status:** Source-level inventory updated for PR #194; compiled API comparison is enforced by package validation  
**Date:** 2026-11-09  
**Purpose:** Record the published baseline, intended 3.0.0 public contract, and migration obligations for the gRPC modernization.

## 1. Published baseline and release policy

The latest published `Franz.Common.Grpc` baseline identified for this work is version **2.3.3**. The repository's working source line is 3.0.0. The published package is a consumer contract, but the user has authorized a full modernization for the 3.0.0 breaking-change line.

The core project now enables .NET package validation against the published 2.3.3 baseline and generates a tracked `CompatibilitySuppressions.xml` for the explicitly authorized 3.0.0 breaking changes. The ASP.NET Core adapter is a new package and has no published baseline. The CI package-consumer step separately validates the packed core/adapter pair.

## 2. Current package boundary

### `Franz.Common.Grpc`

The core package references `Grpc.Core.Api` and owns:
- Immutable logical call context and metadata conventions.
- gRPC outcome and status contracts.
- Unary client and server behavior contracts and pipeline providers.
- Explicit server-streaming, client-streaming, and duplex behavior contracts and providers.
- Tenant, validation, authorization, logging, and bounded metrics abstractions.
- Standard .NET ActivitySource and Meter instrumentation.
- Host-independent dependency-injection registrations.

The core package does not reference `Grpc.Net.Client`, ASP.NET Core hosting, or the ASP.NET Core adapter. It does not create channels or own handlers.

### `Franz.Common.Grpc.AspNetCore`

The adapter owns:
- ASP.NET Core server interceptor and hosting registration.
- Trusted principal-to-call-context mapping.
- Server-call scope and transport lifecycle.
- Bridging between gRPC stream readers/writers and Franz async stream contracts, including per-message response write options.

The adapter references core in one direction only.

### Client transport

Generated typed clients, channels, handlers, credentials, endpoint configuration, and transport disposal belong to `Grpc.Net.ClientFactory` and the application host. The former Franz channel-owning factory has been removed.

The Franz client wrapper governs unary calls only. Generated client streaming, server streaming, and duplex methods remain directly available through the typed client; they do not run through a Franz client-side streaming behavior pipeline. This is an explicit scope boundary documented in `grpc-client-lifecycle.md`.

## 3. Current source-level public surface

### Context and outcome
- `GrpcCallContext`: immutable call context, client/server factories, metadata helpers, deadline/cancellation helpers, and conversion to `Metadata`/`CallOptions`.
- `GrpcOutcome`: success/failure factories and gRPC status/exception conversion.
- Metadata header constants on `GrpcCallContext`.

### Server pipeline
- Unary behavior interfaces and provider with explicit numeric order and duplicate-order rejection.
- Explicit server-streaming, client-streaming, and duplex behavior interfaces/providers with deterministic order and duplicate-order rejection.
- Validation, tenant resolution, authorization, logging, metrics, and exception-mapping behaviors for supported server RPC shapes.
- Streaming logging and metrics cover the complete stream lifetime and do not inspect payloads.
- Tenant resolver/authorizer, validation, logger, and bounded metrics abstractions.
- `GrpcMetricContext` excludes tenant, user, correlation, peer, and request identifiers.
- `FranzGrpcDiagnostics` exposes the standard ActivitySource and Meter.
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

Inert legacy timeout, metadata normalization/propagation, payload logging, retry, method-name, and hosting naming-convention switches were removed from the 3.0.0 source contract. Client timeout validation rejects values less than or equal to zero and values greater than 365 days.

## 4. Compatibility dispositions

| Legacy surface | 3.0.0 disposition | Migration |
|---|---|---|
| Caller-controlled user identity from `x-user-id` | Removed as an identity source | Derive identity from the authenticated host principal |
| `FranzServiceBase.ExecuteUnary` manual pipeline | Removed | Use generated service implementations registered through the ASP.NET Core adapter |
| `IFranzGrpcClientFactory` / `FranzGrpcClientFactory` | Removed; no channel-owning compatibility facade | Use `AddGrpcClient<TClient>` and inject the generated client |
| Legacy broad `Grpc` package dependency | Replaced by `Grpc.Core.Api` in core | Reference the supported API package through the new package graph |
| Inert global/client option properties | Removed | Remove obsolete configuration keys; configure transport retries, credentials, and metadata through supported gRPC host APIs |
| `FranzGrpcHostingOptions.ApplyNamingConventions` | Removed placeholder | Use explicit generated service names and normal ASP.NET Core endpoint conventions |
| Unary-only client behavior wrapper | Retained as unary-only | Use generated typed-client streaming methods directly |
| `IFranzGrpcMetrics` methods accepting `GrpcCallContext` | Replaced with `GrpcMetricContext` | Implement metrics using service, method, RPC shape, and direction; do not tag identity/correlation/request values |
| `IFranzGrpcLogger` | Extended with stream lifecycle methods | Implement start, complete, error, and aborted stream events without logging payloads |
| No-op authorization fallback | Replaced by fail-closed deny-all default | Register an explicit application authorization service |

## 5. Required binary compatibility evidence

Before release:
1. Run `dotnet pack` with package validation enabled against the published 2.3.3 core package.
2. Review every generated `CompatibilitySuppressions.xml` entry and confirm it corresponds to an approved 3.0.0 breaking change; do not suppress unexplained differences.
3. Verify both package dependency graphs, including absence of `Grpc.Net.Client` and ASP.NET Core hosting dependencies from core.
4. Build the gRPC package-consumer fixture from the packed core/adapter pair, using a consumer-owned proto and official typed-client registration.
5. Include the reviewed suppression file, package dependency evidence, and migration examples in release evidence.

Do not mark the compatibility phase complete until these artifacts exist and are reviewed.
