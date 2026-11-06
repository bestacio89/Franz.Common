# Franz.Common.Annotations

> Attribute metadata for declaring Kafka topic requirements on application types.

## Overview

Attribute metadata for declaring Kafka topic requirements on application types.

## Responsibilities

This package currently exposes `RequiredKafkaTopicAttribute`. It is metadata only: it does not discover attributes, validate runtime configuration, create Kafka topics, or register producers and consumers. Those behaviors belong to the consuming package or application.

## Installation

```bash
dotnet add package Franz.Common.Annotations
```

## Attribute contract

The attribute can be applied once to a class. It supports parameterless named-property configuration and two positional constructors.

```csharp
using Franz.Common.Annotations;

// Explicit topic metadata.
[RequiredKafkaTopic(Topic = "orders", DeadLetterTopic = "orders-dlq")]
public sealed class OrderConsumer
{
}

// Format metadata for a consumer-specific convention.
[RequiredKafkaTopic("{0}-in")]
public sealed class CustomerConsumer
{
}

// Format plus entity name; Topic is materialized by the constructor.
[RequiredKafkaTopic("{0}-in", "Customers")]
public sealed class CustomerEventsConsumer
{
}
```

### Properties

| Member | Meaning |
|---|---|
| `Topic` | Explicit or constructor-derived topic name. |
| `Format` | Topic format/convention metadata. |
| `DeadLetterTopic` | Optional dead-letter topic metadata. |

The positional constructors reject a null format. The two-argument constructor formats the supplied pattern with the entity name and stores the resulting value in `Topic`. The one-argument constructor stores the pattern in `Format`; it does not resolve a topic name itself.

## Consumption and validation

A consumer of this metadata must define its own interpretation rules, including how it handles missing values, format placeholders, invalid topic names, and dead-letter topic provisioning. Do not assume that decorating a class triggers validation automatically.

## Package boundaries

This assembly contains the attribute contract only. Kafka provisioning and runtime behavior are implemented by the relevant messaging packages and host application.

## License

MIT. See [LICENSE](../../LICENSE).
