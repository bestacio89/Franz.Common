# Franz.Common.Messaging.Hosting.Azure

`Franz.Common.Messaging.Hosting.Azure` is the **Azure hosting orchestration layer** for the **Franz Framework messaging stack**.

It provides an **opinionated, batteries-included runtime** that wires Azure messaging transports into the **.NET Generic Host**, while preserving Franz’s core principles:

* transport purity
* mediator-driven execution
* explicit lifecycle management
* extensibility through composition

This package **does not implement messaging transports**.
It **orchestrates** them.

---

## 🎯 Purpose

This package answers one simple question:

> *“I want to run Franz messaging on Azure — just make it work.”*

It does so by:

* starting and stopping Azure background consumers
* mapping HTTP endpoints for Azure Event Grid
* reusing the core Franz hosting abstractions
* leaving transport adapters fully reusable and hosting-agnostic

---

## 🧩 What This Package Orchestrates

`Franz.Common.Messaging.Hosting.Azure` coordinates the following transport adapters:

* **Azure Service Bus**
  `Franz.Common.Messaging.AzureEventBus`

* **Azure Event Hubs (streaming)**
  `Franz.Common.Messaging.AzureEventHubs`

* **Azure Event Grid (HTTP ingress)**
  `Franz.Common.Messaging.AzureEventGrid`

Each transport remains **independently usable** outside of this hosting package.

---

## ✨ Features

### 🟦 Azure-Native Hosting

* Uses **.NET Generic Host**
* Registers background listeners as `IHostedService`
* Clean startup / shutdown semantics
* No custom loops or hidden threads

---

### 🧠 Franz Hosting Abstractions Reused

* Builds on **Franz.Common.Messaging.Hosting**
* Reuses:

  * hosting lifecycle conventions
  * Outbox / Inbox listeners
  * mediator dispatch semantics
* No duplication of hosting logic

---

### 🌐 Event Grid HTTP Ingress

* Maps **Azure Event Grid** to HTTP endpoints
* Minimal API–friendly
* Supports subscription validation
* Dispatches events through Franz mediator

---

### 🔌 Transport-Pure Design

* No Azure SDK usage at the business layer
* No transport logic duplicated
* Hosting and transport concerns remain strictly separated

---

## 📦 Dependencies

This package depends on:

### Franz packages

```
Franz.Common.Messaging.Hosting
Franz.Common.Messaging.AzureEventBus
Franz.Common.Messaging.AzureEventHubs
Franz.Common.Messaging.AzureEventGrid
Franz.Common.Logging
Franz.Common.Errors
```

### .NET / ASP.NET

```
Microsoft.Extensions.Hosting
Microsoft.Extensions.DependencyInjection
Microsoft.Extensions.Logging
Microsoft.AspNetCore.Http.Abstractions
```

❌ No business dependencies
❌ No direct Azure SDK usage (delegated to transport packages)

---

## 📂 Project Structure

```
Franz.Common.Messaging.Hosting.Azure/
├── DependencyInjection/
│   └── ServiceCollectionExtensions.cs
│
├── EventBus/
│   └── AzureEventBusHostedService.cs
│
├── EventHubs/
│   └── AzureEventHubsHostedService.cs
│
├── EventGrid/
│   ├── AzureEventGridEndpointOptions.cs
│   └── EndpointRouteBuilderExtensions.cs
│
└── README.md
```

---

## ⚙️ Registration

### Service registration

```csharp
builder.Services.AddFranzAzureHosting(options =>
{
  options.EventBus = bus =>
  {
    bus.ConnectionString = configuration["Azure:ServiceBus"]!;
    bus.EntityName = "franz-events";
  };

  options.EventHubs = hubs =>
  {
    hubs.ConnectionString = configuration["Azure:EventHubs"]!;
    hubs.EventHubName = "orders-stream";
    hubs.ConsumerGroup = "$Default";
    hubs.BlobConnectionString = configuration["Azure:Storage"]!;
    hubs.BlobContainerName = "eventhub-checkpoints";
  };

  options.EventGrid = grid =>
  {
    grid.Route = "/events/eventgrid";
  };
});
```

---

### Event Grid endpoint mapping

```csharp
app.MapFranzAzureEventGrid();
```

Or with customization:

```csharp
app.MapFranzAzureEventGrid(options =>
{
  options.Route = "/integrations/azure/eventgrid";
});
```

---

## 🔄 Runtime Behavior

### Background listeners

On application startup:

* Azure Service Bus processor starts
* Azure Event Hubs processor starts
* Franz Outbox listeners (if registered) start

On shutdown:

* Processors are stopped gracefully
* In-flight messages are completed or retried by Azure

---

### Event Grid ingress

* Azure Event Grid posts events to HTTP endpoint
* Subscription validation handled automatically
* Events mapped to Franz `Message`
* Dispatched through Franz mediator pipelines

---

## 🧠 Design Philosophy

This package is **opinionated but optional**.

You may choose to:

* use this hosting package for rapid Azure adoption
* host Azure transports manually in custom runtimes
* embed Franz messaging in Azure Functions or workers

The transport packages remain **fully reusable** without this hosting layer.

> **Hosting.Azure orchestrates — transports integrate.**

---

## 🚀 Extensibility

This package is designed to evolve:

* Additional Azure transports can be added
* Hosting behavior can be overridden
* Event Grid routing can be customized
* Outbox / Inbox listeners remain transport-agnostic

---

## 📝 Version Information

* **Current Version**: **1.7.0**
* Target Framework: **.NET 10**
* Part of the **Franz Framework**

---

## 📜 License

MIT License — see `LICENSE`.

---

## ✅ Status

This package completes the **Azure runtime layer** for the Franz messaging ecosystem.

Together with:

* `AzureEventBus`
* `AzureEventHubs`
* `AzureEventGrid`

it provides a **full, production-ready Azure messaging stack** with clean separation of concerns and long-term extensibility.

