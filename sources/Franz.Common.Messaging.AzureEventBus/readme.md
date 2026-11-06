# Franz.Common.Messaging.AzureEventBus

> Azure Service Bus producer and processor integration for Franz messages.

## Overview

This package maps between Azure Service Bus messages and the Franz message envelope, registers an `IMessagingSender`, and hosts an Azure Service Bus processor that dispatches received messages through the Franz Mediator.

## Installation

```bash
dotnet add package Franz.Common.Messaging.AzureEventBus
```

## Registration

```csharp
using Franz.Common.Messaging.AzureEventBus.DependencyInjection;

builder.Services.AddFranzAzureEventBus(options =>
{
    options.ConnectionString = builder.Configuration["AzureServiceBus:ConnectionString"]!;
    options.EntityName = "orders";
    options.MaxConcurrentCalls = 8;
    options.PrefetchCount = 0;
});
```

The extension validates the options, registers the Azure SDK client/sender factories, message mapper, producer, processor, consumer, and `AzureEventBusHostedService`.

The processor uses `ConnectionString`, `EntityName`, `MaxConcurrentCalls`, `PrefetchCount`, and `MaxAutoLockRenewalDuration`. The options model also declares session, retry, and dead-letter settings, but the current processor/client construction does not apply them; do not rely on those properties to configure session handling or handler-level retries.

## Tenant-aware processing

The processor creates a DI scope for each message. When `ITenantMessageContextInitializer` is registered, it establishes validated tenant context and resolves `IDispatcher` from that message scope. Without the initializer, the current processor dispatches through the `IDispatcher` captured by its singleton instance rather than the scoped dispatcher from the message scope. Register the messaging multi-tenancy package, tenant store, and producer authorizer when messages carry tenant-owned work.

## DI lifetime limitation

`AddFranzAzureEventBus` registers `AzureEventBusProcessor` as a singleton and constructs it with `IDispatcher`, which Franz Mediator registers as scoped. With DI scope validation enabled, resolving the processor fails because a singleton captures a scoped dependency; with validation disabled, the processor retains a root-scoped dispatcher. The per-message scope only corrects dispatch resolution when tenant initialization is present. Do not treat this processor registration as production-safe until the dispatcher is resolved from each message scope and the singleton dependency is removed.

## Delivery behavior

The processor completes a message after successful dispatch. Mapping failures are dead-lettered unconditionally. Handler failures are logged and the message is abandoned for broker redelivery. `MaxAutoLockRenewalDuration` controls processor lock renewal; configure delivery limits and dead-letter policy on the Service Bus entity because the package's declared retry/dead-letter option objects are not currently applied.

## License

MIT. See the repository [LICENSE](../../LICENSE).
