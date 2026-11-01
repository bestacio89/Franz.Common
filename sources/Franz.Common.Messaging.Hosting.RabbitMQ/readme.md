# Franz.Common.Messaging.Hosting.RabbitMQ

RabbitMQ listener hosting for Franz.Common with scoped message execution.

## Install

```bash
dotnet add package Franz.Common.Messaging.Hosting.RabbitMQ --version 2.3.4
```

## Execution lifecycle

The hosted handler creates a dependency-injection scope for each delivery, establishes message context, initializes validated tenant execution when messaging multi-tenancy is registered, resolves the applicable strategy and awaits completion.

Tenant execution and ambient message context are disposed/cleared after processing, including exception paths. Handler failures propagate to the listener so broker acknowledgement/retry behaviour can reflect failed execution; acknowledgement ownership remains with the transport listener's operational contract.

## Tenant-aware configuration

Register AddMessagingMultitenancyContext(), ITenantStore and the application-specific ITenantMessageAuthorizer. Treat broker headers as untrusted input and authorize the producer against the resolved active tenant.

Use per-delivery scopes. Never share mutable tenant accessors between concurrent RabbitMQ deliveries.

Framework release line: **2.3.4**; target framework: net10.0.

See [RabbitMQ operational contracts](../../docs/architecture/messaging/rabbitmq-operational-contracts.md) and [messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md).

MIT License.
