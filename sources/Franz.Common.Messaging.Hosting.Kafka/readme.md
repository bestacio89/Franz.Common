# Franz.Common.Messaging.Hosting.Kafka

> .NET hosted-service integration for Kafka message listeners and outbox processing.

## Overview

This package owns the generic-host lifecycle for Kafka message consumption and outbox processing. Kafka transport configuration, producers, consumers, topic provisioning, and transaction infrastructure belong to Franz.Common.Messaging.Kafka.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.Hosting.Kafka
~~~

## Registration

Register Kafka transport first, then the hosted listeners:

~~~csharp
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Hosting.Kafka;

services.AddKafkaMessaging(configuration);

services.AddKafkaHostedListener(options =>
{
    options.BootstrapServers = configuration["Messaging:Kafka:BootstrapServers"]!;
    options.GroupId = configuration["Messaging:Kafka:GroupId"];
});

services.AddOutboxHostedListener(options =>
{
    // Configure the OutboxOptions used by the outbox listener.
});
~~~

AddKafkaHostedListener accepts an Action<KafkaMessagingOptions>. It registers one KafkaMessageListener instance as the listener and starts KafkaMessagingHostedService. AddOutboxHostedListener accepts an Action<OutboxOptions>, registers OutboxMessageListener, and starts OutboxHostedService.

Do not pass IConfiguration directly to these hosted-listener methods; their public signatures accept option configuration delegates.

## Execution lifecycle

KafkaMessagingHostedService attaches the message callback, listens until host shutdown, and clears the callback during shutdown. Each delivery is processed in a dependency-injection scope with a MessageContextAccessor. When messaging multi-tenancy is registered, the service establishes and disposes the validated tenant execution context around strategy execution.

Message execution failures are propagated to the listener so the transport does not treat the delivery as successful. The hosted service treats cancellation requested by host shutdown as expected; an unrecoverable listener failure is logged and rethrown so the host observes it.

## Tenant-scoped consumers

For tenant-scoped processing, register Franz.Common.Messaging.MultiTenancy and enable RequireTenantScope. The hosted service validates the tenant boundary at startup. Register ITenantStore and ITenantMessageAuthorizer, and implement application-specific tenant validation where required. A tenant header alone is not authorization.

## Operational boundaries

- This package starts hosted execution; it does not own Kafka topic naming or transport registration.
- Kafka transport must be registered independently through AddKafkaMessaging.
- Handler dependencies must be resolved from the per-delivery scope.
- Handlers should observe cancellation and make external side effects idempotent.
- The outbox listener uses the OutboxOptions contract; configure its polling and execution policy through that options type.

## License

MIT. See the repository [LICENSE](../../LICENSE).
