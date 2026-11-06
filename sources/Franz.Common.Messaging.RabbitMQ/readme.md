# Franz.Common.Messaging.RabbitMQ

> RabbitMQ transport integration for Franz messaging, including confirmed publishing, topology policy, delivery, retries, and dead-letter routing.

## Overview

This package owns RabbitMQ transport infrastructure: connection and channel management, publishing, sending, consumer construction, topology initialization, replay strategy, and transaction support. Hosted listener lifecycle is provided separately by Franz.Common.Messaging.Hosting.RabbitMQ.

RabbitMQ and Kafka share Franz's transport-neutral message contracts, but their broker guarantees, topology, delivery, and transaction semantics are not identical.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.RabbitMQ
~~~

## Registration

~~~csharp
using Franz.Common.Messaging.RabbitMQ.Extensions;

services.AddRabbitMQMessaging(configuration);
~~~

The package binds RabbitMQ options from Messaging:RabbitMQ and registers publisher, sender, consumer, connection/channel pools, message serialization, initializer, and transaction services. The RabbitMQ configuration section is required.

## Configuration

The Messaging:RabbitMQ section controls connection settings, queue/exchange names, provisioning mode, queue type, publisher-confirm timeout, concurrency, message TTL/limits, ordering, failure policy, and replay strategy.

Connection configuration must use either an absolute amqp(s) URI with explicit credentials or the supported explicit host/username/password fields. When TLS is enabled with a URI, use amqps. Keep credentials in secret configuration.

ProvisioningMode controls topology behavior. Development defaults to Create when no mode is specified; other environments default to Verify. Verify requires the management API URL so the configured topology can be inspected. Choose the mode explicitly for production deployments.

## Publisher confirms and replay

RabbitMQ replay strategies require publisher-confirm channels. Each replay operation uses the configured confirm-capable channel pool; replay is not performed through an unconfirmed publish path.

- NoReplayStrategy does not retry the message. When dead-letter-on-exhaustion is enabled, it publishes the failed delivery to the dead-letter queue.
- ProgressiveFourStepReplayStrategy republishes through the configured retry topology using the progressive delay stages, then dead-letters after the configured retry budget when enabled.

The original delivery must not be acknowledged as successfully processed until the replay/dead-letter publication has completed successfully. Confirm failures must remain visible to the caller/consumer lifecycle; a failed publication cannot be treated as a successful replay.

Publisher-confirm timeout is configured through RabbitMQMessagingOptions. Use a bounded timeout appropriate to broker latency and operational recovery.

## Transactions

The RabbitMQ transport exposes IMessagingTransaction for its supported transaction boundary. Use the canonical options-aware registration and constructor path; the removed legacy sender constructor is not supported. CompleteAsync rejects calls after transaction disposal.

Do not infer distributed atomicity between RabbitMQ and an external database. Use an outbox/inbox design when the workflow crosses those persistence boundaries.

## Hosted execution

Register background consumption through Franz.Common.Messaging.Hosting.RabbitMQ. This package's transport registration does not by itself start the generic-host listener.

## Operational constraints

- Configure queue type and topology consistently across deployment environments.
- Keep MaxConcurrency at 1 when strict configured ordering is required.
- FailurePolicy retries require ProgressiveFourStep replay.
- Delivery limits are supported only for quorum queues.
- Use idempotent handlers; redelivery remains possible.
- Tenant-scoped consumers must establish validated delivery context through Franz.Common.Messaging.MultiTenancy. Headers are not authorization.

## Related packages

- Franz.Common.Messaging — transport-neutral contracts.
- Franz.Common.Messaging.Hosting.RabbitMQ — hosted listener and outbox execution.
- Franz.Common.Messaging.MultiTenancy — validated tenant context for delivery.

## License

MIT. See the repository [LICENSE](../../LICENSE).
