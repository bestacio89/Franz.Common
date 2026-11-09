# gRPC Client Lifecycle and Transport

**Status:** Accepted implementation contract for PR #194  
**Scope:** Typed-client ownership, unary pipeline semantics, deadlines, cancellation, TLS/mTLS, metadata, and retry boundaries.

## 1. Ownership and reuse

The application host owns generated typed-client registration, endpoint configuration, credentials, HTTP handlers, and transport lifetime. Use the official `Grpc.Net.ClientFactory` integration:

```csharp
services.AddGrpcClient<Orders.OrdersClient>(options =>
{
    options.Address = new Uri(configuration["Grpc:Orders:Address"]!);
});

services.AddFranzGrpcClient(configuration);
```

A Franz client wrapper receives the generated client and the Franz behavior provider. It owns per-call context and unary behavior execution; it does not create, cache, or dispose channels or handlers.

Do not create a `GrpcChannel` per RPC. Do not dispose a generated client or channel from a Franz behavior. Let the official client factory and the host's dependency-injection container manage transport lifetime.

## 2. Supported client pipeline scope

The current Franz client wrapper and `IGrpcClientBehavior<TRequest,TResponse>` contract govern unary RPC calls. They do not wrap client-streaming, server-streaming, or duplex calls. Applications use the generated typed client's streaming methods directly for those shapes; channel, deadline, cancellation, and metadata configuration remain standard gRPC client concerns.

This is an explicit scope boundary, not a claim that generated streaming methods are unsupported by gRPC. A future Franz client-streaming pipeline requires separate contracts and lifecycle tests and must not be inferred from the unary behavior abstraction.

## 3. Deadlines and cancellation

- `FranzGrpcClientOptions.DefaultClientTimeout` supplies the default deadline for unary calls when no explicit deadline is provided.
- An explicit per-call `deadlineUtc` takes precedence over the configured default.
- The caller's `CancellationToken` is passed through the behavior pipeline.
- Behaviors may forward a linked or narrower cancellation token to their continuation. The terminal transport call uses that effective token when creating `CallOptions`.
- Deadlines are absolute UTC instants. Timeout and cancellation are separate controls.

The configured timeout must be greater than zero and no longer than 365 days. Options validation runs at startup and when options are first accessed.

## 4. TLS and mutual TLS

TLS configuration belongs to the host's standard gRPC client and HTTP handler configuration:

- Use an `https://` endpoint and normal platform certificate validation for server-authenticated TLS.
- For mTLS, configure the official typed client with an `HttpClientHandler` that receives the client certificate from the host's approved certificate/secret provider.
- Keep private keys and certificate passwords out of application configuration files, call metadata, logs, and source control.
- Do not disable certificate validation or install an accept-all validation callback in production.
- Franz does not introduce a parallel certificate store, channel factory, or credential-management abstraction.

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

Certificate acquisition and rotation remain host-specific responsibilities.

## 5. Metadata and identity

The unary Franz client context propagates correlation and request identifiers and, when supplied by the application, tenant/user metadata. Application-specific metadata can be added through the explicit metadata modifier on a unary call.

Metadata is transport input, not proof of identity:

- The server derives authenticated identity from its authenticated principal, never from `x-user-id`.
- `x-tenant-id` is a tenant-selection hint; the server resolves and authorizes membership independently.
- Do not use arbitrary metadata modifiers to transport credentials or secrets. Configure authentication through standard gRPC credentials or HTTP handler mechanisms.
- Do not log authorization metadata, client certificates, or other sensitive values.

## 6. Retry policy

Franz does not automatically retry unary calls. Retry remains disabled unless the host deliberately configures the standard gRPC retry mechanism.

Any retry policy must be method-scoped and restricted to operations whose idempotency and replay semantics are known. Do not retry non-idempotent operations automatically. Streaming calls are not implicitly retryable: partially transmitted or consumed streams cannot be replayed safely without an application-level protocol.

The removed legacy retry-related Franz options never controlled transport retries. Configure retry behavior through the official gRPC client configuration.

## 7. Verification requirements

The client transport checkpoint requires tests or explicit host-level evidence for:

- Typed-client/channel reuse and host-owned disposal.
- Default deadline and explicit-deadline precedence.
- Cancellation propagation through behavior continuations.
- Standard and application metadata propagation without identity trust escalation.
- TLS and mTLS handler configuration using standard factory hooks.
- No implicit retries of non-idempotent operations or streaming calls.
