# Franz.Common.Messaging.Hosting.MediatR

> A hosted-messaging strategy executor that deserializes message envelopes and dispatches commands, queries, or integration events through the Franz dispatcher.

## Overview

A hosted-messaging strategy executor that deserializes message envelopes and dispatches commands, queries, or integration events through the Franz dispatcher.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Hosting.MediatR
```

## What the package actually integrates

Despite the historical package name and older README examples, the current `MessagingStrategyExecuter` uses `Franz.Common.Mediator.Dispatchers.IDispatcher`. It does not use MediatR's `IMediator` or `IRequestHandler` APIs in the implementation shown.

The executor implements `IMessagingStrategyExecuter` from the Franz messaging-hosting contracts. Its `CanExecuteAsync` method returns true when the message headers contain the messaging class-name metadata.

## Dispatch behavior

The executor reads the class name from the message headers, resolves it with `Type.GetType`, deserializes the message body with Newtonsoft.Json, and dispatches according to the resulting runtime type:

| Runtime type | Dispatch operation |
|---|---|
| `IIntegrationEvent` | `IDispatcher.PublishNotificationAsync` |
| `ICommand` | `IDispatcher.SendAsync` |
| `IQuery<object>` | `IDispatcher.SendAsync` |
| Any other type | Throws `TechnicalException` |

The package's `IIntegrationEvent` contract extends the Franz mediator `ICommand` contract. The executor checks integration events before commands so that they are published as notifications.

## Message envelope requirements

The message must contain:
- A class-name header resolvable by the running process.
- A non-null JSON body compatible with the resolved type.

Missing bodies, unresolved types, failed deserialization, and unsupported message types produce `TechnicalException`. The executor runs the configured `IAsyncMessageActionFilter` chain around dispatch and propagates the captured base exception after the filter pipeline completes.

## Trust boundary

The class name is resolved dynamically from message metadata and the body is deserialized into that runtime type. Only process messages from trusted producers and enforce broker-level authentication/authorization, schema governance, and payload validation. Do not accept arbitrary externally supplied type names as safe dispatch instructions.

## Concurrency and lifecycle caveat

The current implementation stores an `IEnumerator<IAsyncMessageActionFilter>` and per-message execution contexts as instance fields. It does not recreate the filter enumerator for each message. Do not register or share this executor as a concurrent singleton without addressing that lifecycle behavior; verify the hosting registration lifetime and concurrent-consumption model.

## Registration

This project contains the strategy implementation; it does not expose the historical `AddMessagingWithMediatR()` registration extension described by the previous guide. Register the executor through the hosting strategy mechanism supported by the current messaging-hosting package.

## License

MIT. See [LICENSE](../../LICENSE).
