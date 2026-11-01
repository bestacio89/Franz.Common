# Franz.Common.Messaging.AzureEventGrid

> Messaging and distributed integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Messaging.AzureEventGrid` package provides messaging and distributed integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Messaging.AzureEventGrid
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

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

* **Current Version:** v2.3.3
* **Target Framework:** **.NET 10**
* Part of the **Franz Framework**

---

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.


---
## 📜 License

MIT License — see `LICENSE`.


---

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).


## 2.3.4 tenant execution boundary notes

This package documentation is retained in full from the existing guide. Tenant-context propagation and execution-scope guarantees depend on the actual integration and registration path; use [the messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md) for the qualified contract. Framework tenant filters do not secure raw SQL or provider-native bulk paths by themselves.
