# Franz.Common.Http.Messaging

> ASP.NET Core MVC integration for Kafka or RabbitMQ messaging transaction coordination and health checks.

## Overview

ASP.NET Core MVC integration for Kafka or RabbitMQ messaging transaction coordination and health checks.

## Installation

```bash
dotnet add package Franz.Common.Http.Messaging
```

## RabbitMQ integration

The RabbitMQ extension is in `Franz.Common.Http.Messaging.Extensions`.

```csharp
using Franz.Common.Http.Messaging.Extensions;

builder.Services.AddMessagingInHttpContext(builder.Configuration);
```

This composes RabbitMQ publisher, sender, and consumer registration through `Franz.Common.Messaging.RabbitMQ`, registers `MessagingTransactionFilter`, and adds the RabbitMQ connection health check. The filter is an MVC async action filter: after the action executes, it calls `IMessagingTransaction.CompleteAsync()` when no exception is present, or `RollbackAsync()` when an exception is present.

The filter coordinates an existing messaging transaction; it does not itself create a broker transaction or guarantee atomicity between an HTTP database transaction and a broker operation.

## Kafka integration

Kafka has a separate extension class and entry point:

```csharp
using Franz.Common.Http.Messaging;

builder.Services.AddKafkaMessagingInHttpContext(builder.Configuration);
```

This delegates producer/consumer setup to `AddKafkaMessaging(configuration)`, registers the same MVC transaction filter, and adds a Kafka health check tagged `messaging`. The current health-check registration creates a default `ConsumerConfig`; supply and validate the actual broker configuration through the messaging package and test the health check against the target deployment.

Kafka and RabbitMQ registration are separate explicit calls. This package does not currently expose a single configuration-driven provider switch.

## Manual registration

The package exposes separate methods for registering the messaging transaction filter and health checks. Use them only when composing those pieces manually; avoid duplicate registrations when the top-level extension has already been called.

## Operational considerations

- Treat health checks as diagnostics, not proof that publish/consume workflows are correct.
- Configure broker credentials and TLS in the owning messaging package.
- Define retry, dead-letter, idempotency, and delivery semantics in the broker-specific messaging layer.
- Keep the HTTP request's success/failure semantics explicit. A filter completion callback is not a distributed transaction across arbitrary resources.

## License

MIT. See [LICENSE](../../LICENSE).
