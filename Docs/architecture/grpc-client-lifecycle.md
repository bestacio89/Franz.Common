# gRPC Client Lifecycle and Transport

**Status:** Implementation contract for PR #194  
**Scope:** Typed-client ownership, deadlines, cancellation, TLS/mTLS, metadata, and retry boundaries.

## 1. Ownership and reuse

The application host owns the generated typed-client registration, endpoint, credentials, HTTP handler configuration, and transport lifetime. Use the official `Grpc.Net.ClientFactory` integration:

```csharp
services.AddGrpcClient<Orders.OrdersClient>(options =>
{
    options.Address = new Uri(configuration["Grpc:Orders:Address"]!);
});

services.AddFranzGrpcClient(configuration);
```

A Franz client wrapper receives the generated client and the Franz behavior provider. It owns per-call context and behavior execution; it does not create, cache, or dispose channels or handlers.

Do not create a `GrpcChannel` for each RPC. Do not dispose a generated client or channel from a Franz behavior. Let the official client factory and the host's dependency-injection container manage the configured transport lifetime.

## 2. Deadlines and cancellation

- `FranzGrpcClientOptions.DefaultClientTimeout` supplies the default deadline for unary calls when no explicit deadline is provided.
- A per-call `deadlineUtc` takes precedence over the configured default.
- Pass the caller's `CancellationToken` into the typed-client method.
- Behaviors may forward a linked or narrower cancellation token to their continuation. The terminal transport call must use that effective token when creating `CallOptions`.
- Deadlines are absolute UTC instants. A timeout is not a replacement for cancellation; both must be propagated.

The configured timeout must be positive and within the application's operational budget. Configuration validation and the disposition of all legacy options are tracked in Phase 5.

## 3. TLS and mutual TLS

TLS configuration belongs to the host's standard gRPC client and HTTP handler configuration:

- Use an `https://` endpoint and the platform's normal certificate validation for server-authenticated TLS.
- For mTLS, configure the official typed client with an `HttpClientHandler` that receives the client certificate from the host's approved certificate/secret provider.
- Keep private keys and certificate passwords out of application configuration files, call metadata, logs, and source control.
- Do not disable certificate validation or install an accept-all validation callback in production.
- Franz does not introduce a parallel certificate store, channel factory, or credential-management abstraction.

Example mTLS handler configuration (certificate acquisition is deliberately host-specific):

```csharp
services.AddGrpcClient<Orders.OrdersClient>(options =>
{
    options.Address = new Uri(configuration["Grpc:Orders:Address"]!);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    handler.ClientCertificates.Add(clientCertificate);
    return handler;
});
```

## 4. Metadata and identity

The Franz call context propagates correlation and request identifiers and, when present, tenant and user metadata. Application-specific metadata may be added through the explicit metadata modifier on the client call.

Metadata is transport input, not proof of identity:

- The server derives authenticated identity from its authenticated principal, never from `x-user-id`.
- `x-tenant-id` is a tenant-selection hint; the server must resolve and authorize membership independently.
- Do not use arbitrary metadata modifiers to transport credentials or secrets. Configure authentication through the standard gRPC credentials or HTTP handler mechanisms.
- Do not log authorization metadata, client certificates, or other sensitive values.

## 5. Retry policy

Franz does not automatically retry calls in its client behavior pipeline. Retry remains disabled unless the host deliberately configures the standard gRPC retry mechanism.

Any retry policy must be method-scoped and restricted to operations whose idempotency and replay semantics are known. Do not retry non-idempotent operations automatically. Streaming calls are not implicitly retryable: a partially transmitted or consumed stream cannot be replayed safely without an application-level protocol.

The legacy retry-related options are subject to the Phase 5 configuration inventory; consumers must not assume that setting an option enables transport retries.

## 6. Verification requirements

The client transport checkpoint requires tests or explicit host-level evidence for:

- Typed-client/channel reuse and host-owned disposal.
- Default deadline and explicit-deadline precedence.
- Cancellation propagation through behavior continuations.
- Standard and application metadata propagation without identity trust escalation.
- TLS and mTLS handler configuration using standard factory hooks.
- No automatic retries by default, and no retry of non-idempotent or streaming operations without an explicit safe policy.
