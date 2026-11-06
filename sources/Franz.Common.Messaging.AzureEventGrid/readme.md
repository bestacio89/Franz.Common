# Franz.Common.Messaging.AzureEventGrid

> Host-agnostic Azure Event Grid ingress, event filtering, mapping, and Mediator dispatch.

## Overview

This package registers the Event Grid ingress service and mapper. It parses Azure Event Grid events, handles subscription-validation event recognition, applies an optional event-type allow-list, maps accepted events into Franz messages, and publishes them through the Mediator dispatcher.

## Installation

```bash
dotnet add package Franz.Common.Messaging.AzureEventGrid
```

## Registration

```csharp
using Franz.Common.Messaging.AzureEventGrid;

builder.Services.AddFranzAzureEventGrid();
```

The current registration method has no configuration callback. It registers a default `AzureEventGridFilterOptions` instance with an empty allow-list, which permits all event types. To replace it with an allow-list:

```csharp
using Franz.Common.Messaging.AzureEventGrid.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

builder.Services.AddFranzAzureEventGrid();

var filters = new AzureEventGridFilterOptions();
filters.AllowedEventTypes.Add("Contoso.OrderCreated");
builder.Services.Replace(ServiceDescriptor.Singleton(filters));
```

## Ingress contract

`IAzureEventGridIngress.IngestAsync(EventGridEvent, cancellationToken)` returns `SubscriptionValidationResult?`. For a subscription-validation event, the result contains the validation code. Filtered events return null; accepted business events are mapped and dispatched as notifications.

The batch overload processes each event in order. The package itself does not expose an HTTP endpoint; use the Azure hosting package to map the endpoint.

## DI lifetime limitation

`AddFranzAzureEventGrid` registers `IAzureEventGridIngress` as a singleton, while its implementation captures `IDispatcher`, which Franz Mediator registers as scoped. With DI scope validation enabled, resolving the ingress fails; with validation disabled, it retains a root-scoped dispatcher. The ingress should be scoped or resolve a dispatcher from a per-request scope before being treated as production-safe.

## Subscription-validation limitation

The current built-in HTTP endpoint in `Franz.Common.Messaging.Hosting.Azure` calls the ingress for each event but discards the returned `SubscriptionValidationResult` and responds with an empty `200 OK`. Azure Event Grid requires a response body containing `validationResponse` for subscription validation. Therefore, the built-in endpoint does not complete the subscription-validation handshake. Use a custom endpoint that serializes the returned validation code until the host endpoint is corrected.

## License

MIT. See the repository [LICENSE](../../LICENSE).
