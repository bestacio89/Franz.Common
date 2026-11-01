# Franz.Common.Messaging.MassTransit

MassTransit and Kafka consumer integration for Franz.Common.

## Install

```bash
dotnet add package Franz.Common.Messaging.MassTransit --version 2.3.4
```

## Kafka consumer overloads

The adapter supports a basic handler overload for handlers that do not require scoped tenant-aware dependency resolution.

For scoped dependency resolution and tenant-aware processing, use the overload that receives the delivery IServiceProvider:

```csharp
await consumer.SubscribeAsync<MyPayload>(
    topic,
    async (services, payload) =>
    {
        var handler = services.GetRequiredService<IMyPayloadHandler>();
        await handler.HandleAsync(payload, cancellationToken);
    },
    cancellationToken);
```

The scoped overload requires IServiceScopeFactory, creates a fresh async scope per delivery and maps Kafka headers into a Franz message envelope. When ITenantMessageContextInitializer is registered, it validates tenant/domain metadata before invoking the handler.

The legacy captured-handler overload fails closed when messaging multi-tenancy is enabled. A captured delegate cannot guarantee dependencies are resolved from the per-delivery scope; use the scoped-handler overload instead.

## Trust boundary

Kafka headers are untrusted transport input. Register AddMessagingMultitenancyContext(), ITenantStore and ITenantMessageAuthorizer. The initializer requires a valid active tenant, applies optional validation, authorizes the producer and validates domain ownership.

Framework release line: **2.3.4**; target framework: net10.0.

See [messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md).

MIT License.
