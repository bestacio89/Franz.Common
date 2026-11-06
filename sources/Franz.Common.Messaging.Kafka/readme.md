# Franz.Common.Messaging.Kafka

> Kafka transport integration for Franz messaging, including producers, consumers, topic provisioning, event subscriptions, and isolated transactions.

## Overview

This package provides Kafka transport infrastructure. It does not start hosted listeners; background execution is provided by Franz.Common.Messaging.Hosting.Kafka.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.Kafka
~~~

## Registration modes

### System mode

Register one service-level topic configuration:

~~~csharp
using Franz.Common.Messaging.Kafka.Extensions;

services.AddKafkaMessaging(configuration);
~~~

The package binds Kafka options from Messaging:Kafka and registers the producer, sender, publisher, consumer factory, consumer, connection/model infrastructure, serializer, topic initializer, and transaction infrastructure.

### Event mode

Event mode derives topic registrations from integration-event subscriptions in the generated messaging manifest:

~~~csharp
services.AddFranzMediator(new[] { typeof(ApplicationAssemblyMarker).Assembly });
services.AddEventBasedKafkaMessaging(configuration);
~~~

Call the Mediator registration before event-based Kafka registration so the generated manifest is available. The explicit overload accepts a params Type[] list; every type must be a concrete IIntegrationEvent and simple event names must be unique. Configured subscriptions are validated against the generated manifest.

## Configuration

The options section is Messaging:Kafka. Important settings include:

- BootstrapServers and GroupId;
- Security.SecurityProtocol, Security.SaslMechanism, Security.SaslUsername/SaslPassword, and TLS certificate paths;
- producer acknowledgements, idempotence, batching, compression, retry, and delivery timeout;
- consumer offset reset, polling, concurrency, session timeout, and maximum poll interval;
- topic partitions, replication factor, and topic configuration;
- dead-letter topic and failure policy;
- ProvisioningMode.

ProvisioningMode controls topic creation/verification behavior. Development defaults to Create when no mode is specified; non-development environments default to Verify. Set the mode explicitly in deployment configuration rather than relying on environment inference.

Use TLS/SASL for production broker connections. Plaintext transport requires explicit configuration and should be restricted to isolated development/test environments.

## Messaging contracts

The transport registers Franz's IMessagingPublisher and IMessagingSender abstractions. Use the publisher for integration events and the sender for point-to-point messages. Kafka-specific topic derivation remains within this package.

Kafka consumers are created through IKafkaConsumerFactory. The underlying Confluent IConsumer<string, string> is the transport consumer; it is not a separate Franz consumer abstraction. Consumer configuration rejects automatic offset commit and automatic offset storage so the processing lifecycle controls offset advancement.

## Transactions

IMessagingTransaction uses a dedicated producer created by IKafkaTransactionProducerFactory. It does not accept the ordinary shared producer. BeginAsync creates the isolated producer and starts a transaction; CompleteAsync commits; RollbackAsync aborts. Disposing an active transaction aborts it and releases the producer. Calls after disposal are rejected.

Do not use the shared non-transactional producer for transactional work. Kafka transactions provide Kafka-to-Kafka atomicity; they do not automatically include an external database transaction.

## Model lifecycle

The Kafka model contract implements IAsyncDisposable. Dispose models and providers asynchronously.

## Hosted execution

Register the hosted integration separately through Franz.Common.Messaging.Hosting.Kafka. The transport package alone does not start background consumers or outbox workers.

## Delivery and rebalance behavior

Handlers must observe cancellation and remain bounded. Partition revocation/loss cancels in-flight work for the affected partitions; cancellation is cooperative. A handler that ignores its token can continue after ownership changes.

Offsets are committed after processing. A handler failure does not advance the original record's offset. A rebalance can still occur after successful handler execution and before commit acceptance, so records can be delivered again. Make side effects idempotent or use an inbox/outbox design.

Do not claim exactly-once behavior for external database effects based solely on Kafka offset commits.

## Related packages

- Franz.Common.Messaging — transport-neutral contracts.
- Franz.Common.Messaging.Hosting.Kafka — hosted listener and outbox execution.
- Franz.Common.Messaging.MultiTenancy — validated tenant context for delivery.
- Franz.Common.Mediator — generated handler manifests used by event mode.

## License

MIT. See the repository [LICENSE](../../LICENSE).
