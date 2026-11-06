# Franz.Common.Messaging.MassTransit

> Minimal MassTransit Kafka rider and Confluent producer/consumer adapters.

## Overview

This package currently provides a registration helper for a MassTransit in-memory bus plus Kafka rider, a Confluent Kafka producer adapter, and a separate Confluent consumer implementation. It is not the same integration path as `Franz.Common.Messaging.Kafka`.

## Installation

```bash
dotnet add package Franz.Common.Messaging.MassTransit
```

## MassTransit rider registration

```csharp
using MassTransit;

builder.Services.AddMassTransitKafkaServices(
    kafkaBootstrapServers: "localhost:9092",
    configureConsumers: rider =>
    {
        rider.AddConsumer<OrdersConsumer>();
    });
```

The helper configures an in-memory MassTransit bus, registers a Kafka rider, invokes the supplied rider-registration callback, and registers a singleton Confluent `IProducer<string, string>` plus scoped `IKafkaProducer`.

The current helper does not expose the Kafka factory-configuration callback needed to declare `TopicEndpoint<TConsumer>(...)`. Registering a consumer type through the callback therefore does not, by itself, establish a Kafka topic subscription. Do not treat this helper as a complete hosted Kafka consumer configuration.

## Separate Confluent consumer

The package also exposes `KafkaConsumer` through `IKafkaConsumer`. Its constructor requires an `IConsumer<string, string>` and `IServiceScopeFactory`. The helper above does not register `IKafkaConsumer`; applications using this path must register and configure the Confluent consumer explicitly.

The consumer deserializes each payload, creates an async DI scope, optionally establishes validated tenant context when the messaging multi-tenancy initializer is registered, invokes the supplied handler, and commits the consumed record after successful handling.

## Production boundary

For a complete Franz Kafka transport with topic provisioning, generated manifest validation, producer/consumer registration, hosted lifecycle, and outbox support, use `Franz.Common.Messaging.Kafka` with `Franz.Common.Messaging.Hosting.Kafka`.

## License

MIT. See the repository [LICENSE](../../LICENSE).
