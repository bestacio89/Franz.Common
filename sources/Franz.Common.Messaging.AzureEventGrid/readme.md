# Franz.Common.Messaging.AzureEventGrid

> Host-independent Azure Event Grid ingress mapped to Franz messages and Mediator notifications.

## Overview

This package is an ingress adapter for push-delivered Azure Event Grid notifications. It is not a publisher, broker, durable queue, or hosted listener. It accepts EventGridEvent instances, handles subscription-validation events, filters event types, maps accepted events to Franz Message instances, and dispatches them through IDispatcher.

ASP.NET Core endpoint mapping and host lifecycle belong to Franz.Common.Messaging.Hosting.Azure.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.AzureEventGrid
~~~

## Registration

~~~csharp
using Franz.Common.Messaging.AzureEventGrid;

services.AddFranzAzureEventGrid();
~~~

AddFranzAzureEventGrid registers:

- IAzureEventGridIngress backed by AzureEventGridIngress;
- AzureEventGridMessageMapper;
- AzureEventGridFilterOptions.

The application must also register Franz Mediator and its notification handlers. The ingress implementation dispatches through IDispatcher.

## Event filtering

AzureEventGridFilterOptions.AllowedEventTypes is an ordinal-ignore-case allow-list. An empty set allows every event type. Disallowed event types are logged and ignored without Mediator dispatch.

The current registration method has no configuration callback. To replace the default filter singleton with an allow-list, register a populated instance after AddFranzAzureEventGrid:

~~~csharp
using Franz.Common.Messaging.AzureEventGrid;
using Franz.Common.Messaging.AzureEventGrid.Configuration;

var filters = new AzureEventGridFilterOptions();
filters.AllowedEventTypes.Add("MyCompany.CustomerCreated");
filters.AllowedEventTypes.Add("MyCompany.OrderConfirmed");

services.AddFranzAzureEventGrid();
services.AddSingleton(filters);
~~~

For a single service resolution, the last registered AzureEventGridFilterOptions instance is used.

## Ingress behavior

For normal events, AzureEventGridIngress:

1. rejects a null event;
2. applies the event-type allow-list;
3. creates a structured logging scope;
4. maps the EventGridEvent to a Franz Message;
5. dispatches the message through IDispatcher.PublishNotificationAsync.

For Microsoft.EventGrid.SubscriptionValidationEvent, it extracts the validation code and returns SubscriptionValidationResult. The validation event is not dispatched to business handlers.

## Hosting and subscription validation

The package itself has no ASP.NET Core or hosting dependency. The optional HTTP endpoint is provided by Franz.Common.Messaging.Hosting.Azure through MapFranzAzureEventGrid.

**Current hosting limitation:** the built-in MapFranzAzureEventGrid endpoint currently discards the SubscriptionValidationResult returned by IAzureEventGridIngress and responds with an empty 200 OK. Azure Event Grid subscription validation requires a response body containing the validationResponse value. Therefore, the built-in endpoint does not currently complete the subscription-validation handshake.

Until that endpoint is corrected, applications creating Event Grid subscriptions must use a custom host endpoint that maps SubscriptionValidationResult.ValidationCode to the required validationResponse response field. Do not treat the built-in endpoint as production-ready for subscription creation.

A custom endpoint must also apply the application's ingress authentication/network policy and propagate request cancellation.

## Dependencies and boundaries

- Azure.Messaging.EventGrid provides the Event Grid event model.
- Franz.Common.Mediator provides notification dispatch.
- Franz.Common.Messaging provides the Franz message envelope.
- Hosting, HTTP routing, and endpoint lifecycle are external to this package.
- Event Grid is push-based ingress; use Kafka, RabbitMQ, Event Hubs, or Service Bus for durable broker semantics appropriate to the workload.

## License

MIT. See the repository [LICENSE](../../LICENSE).
