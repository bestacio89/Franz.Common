# Franz.Common.Messaging.Hosting.Azure

> Combined generic-host lifecycle and ASP.NET Core ingress integration for Azure Service Bus, Event Hubs, and Event Grid.

## Overview

This package composes the Franz Azure transport registrations and starts the long-running Service Bus and Event Hubs consumers with the generic host. It also exposes an ASP.NET Core endpoint extension for Azure Event Grid ingress.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.Hosting.Azure
~~~

## Register Azure transports and hosted services

~~~csharp
using Franz.Common.Messaging.Hosting.Azure.DependencyInjection;

services.AddFranzAzureHosting(options =>
{
    options.EventBus = bus =>
    {
        bus.ConnectionString = configuration["Azure:ServiceBus"]!;
        bus.EntityName = "orders";
        bus.MaxConcurrentCalls = 8;
    };

    options.EventHubs = hubs =>
    {
        hubs.ConnectionString = configuration["Azure:EventHubs:ConnectionString"]!;
        hubs.EventHubName = "orders";
        hubs.ConsumerGroup = "$Default";
        hubs.BlobConnectionString = configuration["Azure:Storage:ConnectionString"]!;
        hubs.BlobContainerName = "eventhub-checkpoints";
    };
});
~~~

AddFranzAzureHosting accepts Action<FranzAzureHostingOptions>. It invokes the Azure Service Bus and Event Hubs transport registrations, registers their hosted services, and registers the Azure Event Grid ingress integration.

The Event Bus and Event Hubs options are configured through their respective delegates. Event Grid route configuration is applied when mapping the endpoint; the EventGrid delegate on FranzAzureHostingOptions is not currently consumed by AddFranzAzureHosting and should not be relied on.

## Map the Event Grid endpoint

~~~csharp
using Franz.Common.Messaging.Hosting.Azure.EventGrid;

app.MapFranzAzureEventGrid(options =>
{
    options.Route = "/api/events";
});
~~~

The endpoint accepts an Event Grid event batch, parses it, and forwards each event to the built-in IAzureEventGridIngress registered by AddFranzAzureHosting. The application remains responsible for protecting the route and applying its ingress network/authentication policy.

## Event Grid subscription-validation limitation

The built-in MapFranzAzureEventGrid endpoint currently ignores the SubscriptionValidationResult returned by IAzureEventGridIngress and responds with an empty 200 OK. Azure Event Grid subscription creation requires a response body containing validationResponse. The ingress service extracts and returns the validation code, but the built-in endpoint does not serialize it into the required response. Do not use this endpoint for subscription creation until that host-level response handling is corrected; use a custom endpoint that maps ValidationCode to validationResponse in the meantime.

## Lifecycle boundaries

- Azure Service Bus transport registration creates its consumer and hosted service.
- Event Hubs transport registration creates the processor; this package adds its hosted service.
- Event Grid is an HTTP ingress endpoint, not a background listener.
- Tenant-scoped message execution requires Franz.Common.Messaging.MultiTenancy and its trusted producer authorization dependencies.
- Broker retries/checkpoints do not make external database effects exactly-once.

## Related packages

- Franz.Common.Messaging.AzureEventBus — Service Bus transport.
- Franz.Common.Messaging.AzureEventHubs — Event Hubs transport.
- Franz.Common.Messaging.AzureEventGrid — Event Grid ingress contracts.
- Franz.Common.Messaging.MultiTenancy — validated tenant context for message processing.

## License

MIT. See the repository [LICENSE](../../LICENSE).
