## **Franz.Common.Messaging.AzureEventGrid**

`Franz.Common.Messaging.AzureEventGrid` is the **Azure Event Grid ingress adapter** for the **Franz Framework messaging stack**.

It provides a **host-agnostic, mediator-driven entry point** for Azure Event Grid events, converting external cloud notifications into **deterministic Franz messages** without leaking HTTP, serialization, or hosting concerns into the domain.

This package is intentionally **not a message broker adapter**.
It is an **ingress transport**, designed for **push-based event delivery**.

---

## 🎯 Purpose

Azure Event Grid is:

* HTTP push–based
* notification-oriented
* non-durable
* externally controlled

Franz treats Event Grid as an **edge ingress**, not as a backbone.

This adapter ensures:

* clean separation between **transport** and **business logic**
* mediator-driven processing
* deterministic metadata propagation
* zero coupling to ASP.NET, Azure Functions, or hosting models

---

## ✨ Features

### 🟦 Azure Event Grid Integration

* Native support for `EventGridEvent`
* Single and batch event ingestion
* Azure system events detection (subscription validation)

---

### 🧠 Franz-Native Semantics

* Converts Event Grid events into `Franz.Common.Messaging.Message`
* Dispatches events through **Franz.Common.Mediator**
* No direct domain deserialization
* Correlation handled via Franz message properties

---

### 🔐 Subscription Validation Handling

* Detects `Microsoft.EventGrid.SubscriptionValidationEvent`
* Extracts validation code
* Returns validation result to the hosting layer
* Prevents validation events from reaching business logic

---

### 🚫 Event Type Filtering

* Optional allow-list of accepted event types
* Deterministic ingress control
* Unknown or disallowed events are safely ignored

---

### 📊 Observability & Diagnostics

* Structured logging with scoped metadata:

  * EventId
  * EventType
  * Subject
  * Topic
* Franz logging conventions
* OpenTelemetry-compatible via mediator pipelines

---

## 📦 Dependencies

This package depends **only** on core Franz building blocks and the Azure Event Grid SDK:

```
Franz.Common.Messaging
Franz.Common.Mediator
Franz.Common.Logging
Franz.Common.Errors
Franz.Common.Headers

Azure.Messaging.EventGrid
```

❌ No ASP.NET dependencies
❌ No hosting logic
❌ No serialization layer
❌ No background workers

---

## 📂 Project Structure

```
Franz.Common.Messaging.AzureEventGrid/
├── Configuration/
│   └── AzureEventGridFilterOptions.cs
│
├── Constants/
│   ├── AzureEventGridHeaders.cs
│   └── AzureEventGridEventTypes.cs
│
├── Ingress/
│   ├── IAzureEventGridIngress.cs
│   └── AzureEventGridIngress.cs
│
├── Logging/
│   └── AzureEventGridLogScope.cs
│
├── Mapping/
│   └── AzureEventGridMessageMapper.cs
│
├── Models/
│   └── SubscriptionValidationResult.cs
│
├── DependencyInjection/
│   └── ServiceCollectionExtensions.cs
│
└── README.md
```

---

## ⚙️ Dependency Injection

```csharp
services.AddFranzAzureEventGrid(filter =>
{
    filter.AllowedEventTypes.Add("MyCompany.CustomerCreated");
    filter.AllowedEventTypes.Add("MyCompany.OrderConfirmed");
});
```

Registers:

* `IAzureEventGridIngress`
* `AzureEventGridMessageMapper`
* `AzureEventGridFilterOptions`

Hosting is intentionally **not included**.

---

## 🔄 Ingress Flow

### Normal Event

1. Event Grid pushes HTTP event
2. Hosting layer forwards `EventGridEvent` to ingress
3. Event type is validated
4. Event is mapped to Franz `Message`
5. Message is dispatched via mediator
6. Business handlers execute

---

### Subscription Validation Event

1. Event Grid sends validation request
2. Adapter detects system event
3. Validation code is extracted
4. Hosting layer echoes validation response
5. Event is **not** dispatched to mediator

---

## 🧭 Hosting Examples

### ASP.NET Minimal API

```csharp
app.MapPost("/eventgrid", async (
    EventGridEvent[] events,
    IAzureEventGridIngress ingress) =>
{
    foreach (var evt in events)
    {
        var validation = await ingress.IngestAsync(evt);
        if (validation != null)
            return Results.Ok(validation);
    }

    return Results.Ok();
});
```

### Azure Function

```csharp
public async Task Run(
    [EventGridTrigger] EventGridEvent evt,
    IAzureEventGridIngress ingress)
{
    await ingress.IngestAsync(evt);
}
```

---

## 🚀 Extensibility

Future enhancements include:

* Schema validation per event type
* Event version routing
* Event Grid → Outbox forwarding
* Dead-letter forwarding strategies
* Event Grid domain event projections

---

## 📝 Version Information

* **Current Version**: 1.7.5
* **Target Framework:** **.NET 10**
* Part of the **Franz Framework**

---

## 📜 License

MIT License — see `LICENSE`.


---


