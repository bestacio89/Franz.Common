# Franz.Common.Grpc.AspNetCore

> ASP.NET Core server transport integration that applies the Franz gRPC execution pipeline to incoming unary RPCs.

## Overview

This package connects the hosting-runtime-independent `Franz.Common.Grpc` contracts to ASP.NET Core gRPC hosting. It registers a server interceptor, resolves behavior providers within a per-call asynchronous dependency-injection scope, and derives the trusted Franz user identifier from a configured claim on an authenticated server-side principal. Authentication remains the host's responsibility, and caller-controlled `x-user-id` metadata is never an identity authority.

## Installation

```bash
dotnet add package Franz.Common.Grpc.AspNetCore
```

## Registration

Register the core server pipeline and its defaults, then register the ASP.NET Core adapter:

```csharp
using Franz.Common.Grpc.DependencyInjection;
using Franz.Common.Grpc.AspNetCore.DependencyInjection;

builder.Services
    .AddFranzGrpcServer(builder.Configuration)
    .AddFranzGrpcDefaults()
    .AddFranzGrpcAspNetCore();
```

The default authorization implementation denies calls. Register an explicit `IFranzAuthorizationService` policy for the application before exposing endpoints. Configure ASP.NET Core authentication and authorization middleware in the host; this adapter consumes the resulting authenticated principal and does not establish authentication itself.

## Trusted identity

The default subject claim is `ClaimTypes.NameIdentifier`. Override it when the host's authentication scheme uses a different stable subject claim:

```csharp
builder.Services.AddFranzGrpcAspNetCore(options =>
{
    options.UserIdClaimType = "sub";
});
```

A user identifier is copied into `GrpcCallContext` only when the server-side principal is authenticated and contains the configured claim. Request metadata is not consulted for identity.

## Pipeline scope

The interceptor applies the ordered unary behavior pipeline at the transport boundary, so ordinary generated service implementations do not need to inherit a Franz-specific service base or invoke the pipeline manually. Every unary call receives its own asynchronous DI scope, which remains alive until the handler and all unary behaviors complete.

This initial adapter checkpoint wires unary RPCs. Server-streaming, client-streaming, and duplex-streaming transport integration remain separate implementation work. The package must not be considered production-complete for streaming until its scope spans deferred enumeration, response writes, cancellation, exceptions, and disposal.

## License

MIT. See the repository [LICENSE](../../LICENSE).
