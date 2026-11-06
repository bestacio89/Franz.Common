# Franz.Common.Messaging.MassTransit

> MassTransit Kafka-rider setup and a separate Confluent Kafka producer/consumer contract.

## Overview

This package currently contains two distinct Kafka integration paths:

- MassTransit registration through the Kafka rider.
- Franz's IKafkaProducer and IKafkaConsumer contracts backed by Confluent.Kafka.

These paths are not automatically bridged by the registration helper. The package name does not imply that the Confluent consumer contract is implemented by MassTransit.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.MassTransit
~~~

## MassTransit registration

~~~csharp
services.AddMassTransitKafkaServices(
    kafkaBootstrapServers: configuration["Kafka:BootstrapServers"]!,
    configureConsumers: rider =>
    {
        rider.AddConsumer<OrderConsumer>();
    });
~~~

AddMassTransitKafkaServices configures an in-memory MassTransit bus, registers a Kafka rider host, and registers a Confluent Kafka string producer as IKafkaProducer.

The configureConsumers callback receives IRiderRegistrationConfigurator. The helper itself configures the Kafka host but does not expose the IRiderConfigurator callback needed to define topic endpoints. Registering a consumer type through this callback alone does not establish a topic endpoint. Do not treat this helper as a complete MassTransit consumer-hosting setup until that endpoint configuration gap is addressed.

## Producer contract

IKafkaProducer exposes:

~~~csharp
Task PublishAsync<T>(
    T message,
    string topic,
    CancellationToken cancellationToken = default);
~~~

KafkaProducer serializes the message to JSON, creates a GUID string key, and publishes a string-valued record through the registered Confluent producer. It rejects a null message and blank topic.

AddMassTransitKafkaServices registers IKafkaProducer. It does not register IKafkaConsumer or a Confluent IConsumer<string,string>.

## Separate Confluent consumer contract

IKafkaConsumer exposes:

~~~csharp
Task SubscribeAsync<T>(
    string topic,
    Func<IServiceProvider, T, Task> handler,
    CancellationToken cancellationToken = default);
~~~

KafkaConsumer is a separate Confluent consumer implementation. It requires an IConsumer<string,string> and IServiceScopeFactory. The registration helper does not create or register those dependencies, so applications choosing this path must configure and register the Confluent consumer and IKafkaConsumer explicitly.

For each delivery, KafkaConsumer deserializes the JSON payload, creates an async DI scope, optionally establishes tenant context through ITenantMessageContextInitializer, invokes the handler with the scoped IServiceProvider, and commits only after the handler succeeds. A failure before commit leaves the record eligible for redelivery. Handlers must be idempotent and observe cancellation.

## Tenant-aware processing

When the Confluent consumer path is used with Franz.Common.Messaging.MultiTenancy, register the tenant store and ITenantMessageAuthorizer. The consumer initializes the validated tenant context from message headers before invoking the handler. A tenant header alone is not authorization.

## Integration boundaries

- MassTransit rider registration and the direct Confluent consumer are separate execution paths.
- The helper registers the Confluent producer, not the Confluent consumer.
- The current helper does not expose topic-endpoint configuration for its MassTransit rider.
- The direct Confluent consumer requires explicit consumer registration and configuration.
- Kafka delivery is repeatable; neither path provides exactly-once effects for an external database.

## License

MIT. See the repository [LICENSE](../../LICENSE).
