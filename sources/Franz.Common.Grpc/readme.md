# Franz.Common.Grpc

> gRPC protocol context and Franz client/server behavior pipelines with explicit host and transport ownership.

## Overview

The package supplies Franz behavior pipelines and shared context for gRPC calls. It deliberately does not register ASP.NET Core gRPC endpoints or generated typed clients; the host application owns those framework registrations.

## Installation

```bash
dotnet add package Franz.Common.Grpc
```

## Dependency injection

Register the default no-op services when an application does not yet provide custom validation, authorization, tenant resolution, logging, or metrics:

```csharp
using Franz.Common.Grpc.DependencyInjection;

builder.Services.AddFranzGrpcDefaults();
builder.Services.AddFranzGrpcServer(builder.Configuration);
builder.Services.AddFranzGrpcClient(builder.Configuration);
```

The registrations are independent; add only the client or server side used by the process.

### Server side

`AddFranzGrpcServer(configuration)` binds `Franz:Grpc` and registers the ordered server behaviors:

1. Exception mapping (outermost boundary)
2. Validation
3. Tenant resolution
4. Authorization
5. Logging
6. Metrics

The exception mapper encloses the complete behavior pipeline so failures from validation, tenancy, authorization, logging, metrics, and the service handler reach the same transport error boundary.

The host must register its gRPC server services and map its generated service implementations:

```csharp
builder.Services.AddGrpc();
// Register application services and Franz behavior dependencies.
```

The application provides implementations of `IFranzValidationEngine`, `IFranzAuthorizationService`, `IFranzTenantResolver`, `IFranzGrpcLogger`, and `IFranzGrpcMetrics` according to its security and operational requirements. When a tenant is resolved, `IFranzTenantAuthorizer` and an execution-context accessor are also required; with `RequireTenant = true`, an `ITenantValidator` is required as well. The default no-op resolver is not an authorization policy. With `RequireTenant = true`, an unresolved tenant is denied; if a tenant is resolved, the authorizer and execution-context accessor must be registered, and the validator is also mandatory.

### Client side

`AddFranzGrpcClient(configuration)` binds `Franz:Grpc:Client` and registers the Franz client behavior/provider. Channel creation, handler configuration, and typed-client lifetime are owned by the official `Grpc.Net.ClientFactory` integration. Register generated clients directly with `AddGrpcClient<TClient>`:

```csharp
builder.Services.AddGrpcClient<Orders.OrdersClient>(options =>
{
    options.Address = new Uri(
        builder.Configuration["Grpc:Services:Orders:Address"]!);
});
```

The package does not construct channels or clients. The former `IFranzGrpcClientFactory` and `FranzGrpcClientFactory` APIs are removed; migrate to the official typed-client registration above. The `FranzGrpcClient<TClient>` wrapper can still receive an injected generated client when the application wants to use the Franz client pipeline.

## GrpcCallContext

`GrpcCallContext` is an immutable client/server call context containing correlation ID, request ID, optional tenant/user identifiers, method/service names, peer, deadline, cancellation token, and request/response metadata.

Use `GrpcCallContext.CreateClient(...)` to create outgoing context and `GrpcCallContext.FromServerCallContext(...)` to adapt an incoming server call. The context can produce gRPC metadata and `CallOptions`, and exposes immutable `WithTenant`, `WithUser`, `WithDeadline`, `WithCancellation`, and `WithAdditionalHeader` helpers.

## Tenant trust boundary

The server tenant behavior treats caller-supplied `x-tenant-id` as an untrusted selection hint and makes it available to the application-provided tenant resolver. The resolver must resolve that selector against trusted server-side data; the supplied value is never accepted directly as validated tenant context. After resolution, `IFranzTenantAuthorizer` independently verifies that the authenticated principal belongs to the resolved tenant. Only then is the resolved tenant placed into the effective call context and tenant execution scope. A missing tenant is rejected when `RequireTenant = true`.

Caller-supplied `x-user-id` metadata is removed and never used as identity. The authenticated principal supplied by the host remains the only source of the trusted Franz user identifier.

On the client, the tenant behavior propagates only the tenant from a validated `ITenantExecutionContextAccessor`. It does not trust a caller-created tenant header when no validated execution context exists.

When a validated HTTP tenant context already exists for the same request, the gRPC server behavior can reuse it. For gRPC-only hosts, the gRPC behavior owns its scoped tenant execution context lifecycle.

## Hosting boundary

The package does not call `AddGrpc()`, map endpoints, or configure ASP.NET Core routing. The ASP.NET Core adapter and transport-enforced pipeline are being introduced as a separate package in this modernization workstream.

## License

MIT. See the repository [LICENSE](../../LICENSE).
