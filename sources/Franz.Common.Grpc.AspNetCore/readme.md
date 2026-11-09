# Franz.Common.Grpc.AspNetCore

> ASP.NET Core gRPC server adapter that applies Franz behaviors at the transport boundary for all four RPC shapes.

## Overview

This package connects the hosting-runtime-independent `Franz.Common.Grpc` contracts to ASP.NET Core gRPC hosting. Its interceptor applies the Franz pipelines to unary, server-streaming, client-streaming, and duplex calls. Each RPC receives an asynchronous dependency-injection scope that remains alive for the complete handler and stream lifetime.

The adapter derives the trusted Franz user identifier from a configured claim on an authenticated server-side principal. Authentication remains the host's responsibility. Caller-controlled `x-user-id` metadata is never an identity authority.

## Installation

```bash
dotnet add package Franz.Common.Grpc.AspNetCore
```

## Registration

Register the core server pipeline and defaults, then register the ASP.NET Core adapter:

```csharp
using Franz.Common.Grpc.DependencyInjection;
using Franz.Common.Grpc.AspNetCore.DependencyInjection;

builder.Services
    .AddFranzGrpcDefaults()
    .AddFranzGrpcServer(builder.Configuration)
    .AddFranzGrpcAspNetCore();

builder.Services.AddGrpc();
```

The default authorization implementation denies calls. Register an explicit `IFranzAuthorizationService` policy before exposing endpoints. Configure ASP.NET Core authentication and authorization middleware in the host; this adapter consumes the resulting authenticated principal and does not establish authentication itself.

## Trusted identity

The default subject claim is `ClaimTypes.NameIdentifier`. Override it when the host's authentication scheme uses a different stable subject claim:

```csharp
builder.Services.AddFranzGrpcAspNetCore(options =>
{
    options.UserIdClaimType = "sub";
});
```

A user identifier is copied into `GrpcCallContext` only when the server-side principal is authenticated and contains the configured claim. Request metadata is not consulted for identity.

## RPC pipelines and lifecycle

The interceptor enforces the ordered Franz pipeline for:

| RPC shape | Pipeline contract | Lifetime boundary |
|---|---|---|
| Unary | Unary server behaviors | Handler completion |
| Server streaming | Server-streaming behaviors | Response enumeration and transport writes |
| Client streaming | Client-streaming behaviors | Complete request consumption and handler completion |
| Duplex streaming | Duplex behaviors | Request consumption and response enumeration |

Streaming scopes remain alive until the complete RPC has completed, failed, been cancelled, or been disposed. Tenant execution context is established only after resolution and membership authorization and is cleared when the scope ends. Exception mapping encloses stream enumeration and sanitizes unexpected failures.

Server-streaming and duplex response writes preserve the service's per-message `WriteOptions`, including compression and buffering flags, across the adapter's bounded backpressure bridge.

The adapter does not own client channels, HTTP handlers, TLS certificates, or client credentials. Those remain the responsibility of the host's official `Grpc.Net.ClientFactory` typed-client configuration.

## License

MIT. See the repository [LICENSE](../../LICENSE).
