# Franz.Common.Messaging.MultiTenancy

Validated tenant execution context for Franz messaging producers and consumers.

## Install

```bash
dotnet add package Franz.Common.Messaging.MultiTenancy --version 2.3.4
```

## Responsibilities

- Registers scoped tenant/domain accessors and ITenantMessageContextInitializer.
- Registers tenant and domain message builders through AddMessagingMultitenancyContext.
- Propagates tenant/domain headers only from validated execution context.
- Removes stale domain metadata when the current context has no domain.
- Resolves and validates inbound tenant metadata before consumer execution.

## Registration

```csharp
services.AddMessagingMultitenancyContext();
```

The application must also register ITenantStore and ITenantMessageAuthorizer. Register ITenantValidator when additional application-specific validation is required.

## Producer contract

Message builders read ITenantExecutionContextAccessor. They do not trust arbitrary tenant IDs supplied by application payloads. A tenant header is emitted from validated tenant context; a domain header is emitted only when a validated domain is present.

## Consumer contract

ITenantMessageContextInitializer.BeginAsync:

1. Requires exactly one valid tenant identifier in message headers.
2. Resolves the tenant from ITenantStore and requires active status.
3. Invokes the optional ITenantValidator.
4. Authorizes the producer through ITenantMessageAuthorizer.
5. Validates that any supplied domain is verified and belongs to the resolved tenant.
6. Initializes the scoped tenant execution accessor and returns an IAsyncDisposable that clears it.

The initializer supports ambient message context and an explicit Message argument. Custom implementations must implement the explicit-message overload to participate in transport integrations that pass the message directly. Its default interface implementation throws NotSupportedException: source compatibility is retained while unsupported execution fails closed.

## Transport integrations

Tenant initialization is invoked automatically in generic messaging, Kafka, RabbitMQ, Azure Service Bus, Azure Event Hubs, Outbox and MassTransit Kafka consumer paths when registered. Each delivery must use its own DI scope. Tenant scope is disposed after the handler pipeline, including failure and cancellation.

The MassTransit Kafka adapter's scoped-handler overload receives the per-delivery IServiceProvider. Its legacy captured-handler overload fails closed when tenant-aware processing is enabled.

## Operational requirements

Tenant headers are untrusted transport input. Use authenticated broker identities, producer authorization and tenant-store validation. Do not treat a tenant header as proof that a producer may act for that tenant. Background jobs must re-enter through the same validation boundary and must not reuse an HTTP-scoped accessor.

Framework release line: **2.3.4**; target framework: net10.0.

See [messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md) and [tenant isolation audit](../../Docs/tenant-isolation-boundary-audit.md).

MIT License.
