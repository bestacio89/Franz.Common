# Franz.Common.Messaging.Hosting.Kafka

Kafka listener hosting for Franz.Common with per-delivery execution scopes.

## Install

```bash
dotnet add package Franz.Common.Messaging.Hosting.Kafka --version 2.3.4
```

## Execution lifecycle

The Kafka hosted service creates a fresh dependency-injection scope for each delivery, establishes message context, initializes tenant execution when Franz.Common.Messaging.MultiTenancy is registered, selects the matching messaging strategy and awaits completion.

Tenant execution is disposed and ambient message context is cleared after the complete handler pipeline, including failure paths. Handler failures are logged and rethrown so the transport does not treat unsuccessful processing as successful completion. Concurrent deliveries must not share mutable scoped accessors.

## Tenant-aware configuration

Register AddMessagingMultitenancyContext(), the application-specific tenant store and ITenantMessageAuthorizer. Producers must propagate tenant metadata from validated execution context. Consumers resolve and authorize metadata before invoking handlers.

Do not use a singleton tenant accessor or capture request-scoped services in a long-lived Kafka listener.

Framework release line: **2.3.4**; target framework: net10.0.

See [messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md).

MIT License.
