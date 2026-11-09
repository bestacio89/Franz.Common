# Franz.Common.Grpc

> Hosting-runtime-independent gRPC call context and ordered client/server behavior pipelines.

## Overview

The core package owns the Franz call context, execution behaviors, error semantics, and pipeline composition. It does not register ASP.NET Core endpoints, create channels, configure HTTP handlers, or own generated typed-client lifetimes.

## Installation

```bash
dotnet add package Franz.Common.Grpc
```

## Dependency injection

Register the no-op server dependencies where the application has not supplied its own implementations, then add the required side(s):

```csharp
using Franz.Common.Grpc.DependencyInjection;

builder.Services
    .AddFranzGrpcDefaults()
    .AddFranzGrpcServer(builder.Configuration)
    .AddFranzGrpcClient(builder.Configuration);
```

The default authorization service denies calls. Replace it with an explicit application policy before exposing server endpoints. Validation, logging, metrics, and tenant resolution defaults are no-op implementations; replace them with application implementations as required.

### Server pipeline

`AddFranzGrpcServer(configuration)` binds `Franz:Grpc` and registers the ordered unary pipeline:

1. Exception mapping (outermost)
2. Validation
3. Tenant resolution
4. Authorization
5. Logging
6. Metrics

It also registers exception mapping, validation, tenant resolution, authorization, logging, and metrics for server-streaming, client-streaming, and duplex RPCs. Server-streaming validates its single request; client-streaming and duplex validate each request message as the downstream handler consumes it. Streaming behaviors execute for the complete stream lifetime, including enumeration, cancellation, and disposal. Streaming logging records lifecycle events only; it does not enumerate or log payloads.

The host registers ASP.NET Core gRPC hosting and maps its generated service implementations. Add `Franz.Common.Grpc.AspNetCore` to apply these pipelines at the transport boundary.

### Client pipeline

`AddFranzGrpcClient(configuration)` binds `Franz:Grpc:Client` and registers the unary client pipeline:

1. Exception mapping
2. Validation
3. Validated tenant propagation
4. Logging
5. Metrics

Client authorization is opt-in: register `AuthorizationClientBehavior<TRequest,TResponse>` when the application has an explicit outbound authorization policy. It is not enabled by default because the standard authorization fallback is fail-closed.

Use the official `Grpc.Net.ClientFactory` typed-client registration. The host owns channel, handler, endpoint, TLS/mTLS credentials, and disposal:

```csharp
builder.Services.AddGrpcClient<Orders.OrdersClient>(options =>
{
    options.Address = new Uri(
        builder.Configuration["Grpc:Services:Orders:Address"]!);
});
```

`FranzGrpcClient<TClient>` and `FranzGrpcClientBase<TClient>` wrap unary calls only. Outgoing streaming calls should use the generated typed client's streaming APIs directly; Franz does not currently expose a client-side streaming behavior pipeline. The server-side streaming contracts are independent and cover all three streaming RPC shapes.

The former channel-owning `IFranzGrpcClientFactory` and `FranzGrpcClientFactory` APIs are removed. Migrate channel creation to `AddGrpcClient<TClient>` and pass the injected generated client into the Franz wrapper for unary calls.

## Configuration

Only options with implemented runtime semantics remain in the 3.0.0 configuration surface:

| Section | Property | Default | Semantics |
|---|---|---|---|
| `Franz:Grpc` | `RequireTenant` | `false` | Require server-side tenant resolution, validation, and membership authorization |
| `Franz:Grpc:Client` | `DefaultClientTimeout` | `00:00:15` | Default unary client deadline when no explicit deadline is supplied |

The client timeout must be greater than zero and no longer than 365 days. Invalid values fail options validation at startup. Legacy timeout, retry, metadata, payload-logging, and naming-convention switches were removed because they did not control production behavior. Configure retries and credentials through the standard gRPC host/typed-client mechanisms.

## Observability

The package exposes standard .NET diagnostics through `FranzGrpcDiagnostics`:

- Activity source: `FranzGrpcDiagnostics.ActivitySourceName` (`Franz.Common.Grpc`)
- Meter: `FranzGrpcDiagnostics.MeterName` (`Franz.Common.Grpc`)
- Counter: `franz.grpc.calls`
- Histogram: `franz.grpc.call.duration` (seconds)

Subscribe to the activity source and meter through the host's OpenTelemetry configuration. Tags are limited to RPC service, method, shape, direction, and outcome. Tenant IDs, user IDs, correlation IDs, peer addresses, and request IDs are excluded from metric dimensions. Custom `IFranzGrpcMetrics` implementations receive `GrpcMetricContext`, which exposes only those bounded dimensions.

## Call context and security

`GrpcCallContext` is an immutable call context containing correlation and request identifiers, optional tenant/user identifiers, method/service names, peer, deadline, cancellation, and metadata.

Use `GrpcCallContext.CreateClient(...)` for outgoing context and `GrpcCallContext.FromServerCallContext(...)` for incoming server calls. Caller-supplied `x-user-id` is never an identity authority. The ASP.NET Core adapter derives the trusted user identifier only from an authenticated server-side principal. Caller-supplied `x-tenant-id` is a selector only; the server resolves the tenant and independently authorizes membership before establishing tenant execution context.

## Error semantics

The outermost exception mapper maps known platform and Franz failures to deliberate gRPC status codes. Cancellation and deadline expiration remain distinct from unexpected failures. Unexpected exception messages and stack traces are not disclosed in remote status details. Error trailers are restricted to approved error and correlation metadata.

## License

MIT. See the repository [LICENSE](../../LICENSE).
