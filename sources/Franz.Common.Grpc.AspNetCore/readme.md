# Franz.Common.Grpc.AspNetCore

ASP.NET Core hosting integration for the Franz gRPC execution pipeline.

## Registration

Register the core server pipeline and its defaults, then register the ASP.NET Core adapter:

```csharp
builder.Services
    .AddFranzGrpcServer(builder.Configuration)
    .AddFranzGrpcDefaults()
    .AddFranzGrpcAspNetCore();
```

The adapter installs a gRPC server interceptor and resolves unary behavior pipelines from a per-call asynchronous dependency-injection scope. It derives the Franz user identifier only from the configured claim on an authenticated ASP.NET Core principal. Caller-supplied `x-user-id` metadata is never trusted as identity.

Authentication and authorization policy remain host responsibilities. The core authorization service remains fail-closed unless the host deliberately registers an explicit policy.

## Scope

The initial adapter release wires unary transport calls. Server-streaming, client-streaming, and duplex-streaming transport integration must be completed before this package is considered production-complete. The streaming adapter must retain the call scope through complete enumeration, writes, cancellation, exceptions, and disposal.
