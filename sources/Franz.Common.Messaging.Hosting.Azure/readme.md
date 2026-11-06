# Franz.Common.Messaging.Hosting.Azure

> Azure transport composition and hosted-service integration for Service Bus and Event Hubs, plus Event Grid endpoint mapping.

## Overview

This package provides `AddFranzAzureHosting(...)` to compose Azure Service Bus, Event Hubs, and Event Grid services, and `MapFranzAzureEventGrid(...)` to map an HTTP ingress endpoint.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Hosting.Azure
```

## Azure hosting registration

```csharp
using Franz.Common.Messaging.Hosting.Azure.DependencyInjection;

builder.Services.AddFranzAzureHosting(options =>
{
    options.EventBus = bus =>
    {
        bus.ConnectionString = builder.Configuration["AzureServiceBus:ConnectionString"]!;
        bus.EntityName = "orders";
    };

    options.EventHubs = hubs =>
    {
        hubs.ConnectionString = builder.Configuration["EventHubs:ConnectionString"]!;
        hubs.EventHubName = "orders";
        hubs.ConsumerGroup = "$Default";
        hubs.BlobConnectionString = builder.Configuration["EventHubs:BlobConnectionString"]!;
        hubs.BlobContainerName = "eventhub-checkpoints";
    };
});
```

The Event Bus and Event Hubs callbacks are forwarded to their transport registration methods. The `EventGrid` callback on `FranzAzureHostingOptions` is currently not consumed by `AddFranzAzureHosting`; configure the HTTP route directly when mapping the endpoint.

## Event Grid endpoint mapping

```csharp
using Franz.Common.Messaging.Hosting.Azure.EventGrid;

app.MapFranzAzureEventGrid(options =>
{
    options.Route = "/events/eventgrid";
});
```

The endpoint accepts an Event Grid event batch, passes each event and the request cancellation token to `IAzureEventGridIngress`, and returns an empty `200 OK`.

**Subscription validation is not complete in the current endpoint.** The ingress returns a `SubscriptionValidationResult`, but the endpoint discards it. Azure Event Grid requires the response body to contain `validationResponse`; use a custom endpoint for subscription creation until this is corrected.

## Composition and lifetime limitations

The current `AddFranzAzureEventBus` transport registration already adds `AzureEventBusHostedService`. `AddFranzAzureHosting` also adds that hosted service after calling the transport registration, resulting in duplicate Event Bus hosted-service registrations. Direct transport registration also registers the hosted service and should not be combined with the wrapper.

The Event Bus and Event Hubs processors are registered as singletons while capturing `IDispatcher`, which Franz Mediator registers as scoped. The Event Grid ingress is also registered as a singleton and captures the scoped dispatcher. These registrations fail when DI scope validation is enabled and retain root-scoped dispatchers when validation is disabled. The Event Bus and Event Hubs processors resolve a scoped dispatcher from each message/event scope only when the tenant message initializer is present. Treat these Azure processor/ingress paths as requiring source-level lifetime correction before production use.

## Related packages

- `Franz.Common.Messaging.AzureEventBus`
- `Franz.Common.Messaging.AzureEventHubs`
- `Franz.Common.Messaging.AzureEventGrid`

## License

MIT. See the repository [LICENSE](../../LICENSE).
