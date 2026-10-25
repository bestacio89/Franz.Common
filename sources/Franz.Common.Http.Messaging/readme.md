# Franz.Common.Http.Messaging

> Messaging and distributed integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Http.Messaging` package provides messaging and distributed integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Http.Messaging
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

Absolutely ✅ — here’s your **fully updated `Franz.Common.Http.Messaging` README** rewritten to match the structure you provided, with the **new v1.6.17** changelog compacted but still complete and consistent with your ecosystem documentation style.

---

# **Franz.Common.Http.Messaging**

A specialized library within the **Franz Framework**, designed to streamline the integration of messaging systems, health checks, and transaction management for HTTP-based services in **ASP.NET Core** applications.
This package ensures seamless messaging health monitoring and transactional consistency across distributed systems — now supporting **Kafka**, **RabbitMQ**, and (future) **Azure EventBus** providers.

---

## **Features**

* **Unified Messaging Integration**

  * Centralized orchestration for Kafka and RabbitMQ through `AddMessagingInHttpContext()`.
* **Messaging Health Checks**

  * Auto-detects and registers protocol-specific health checks (`RabbitMQHealthCheck`, `KafkaHealthCheck`).
* **Transaction Management**

  * `MessagingTransactionFilter` for consistent commit/rollback handling on HTTP requests.
* **Dependency Injection**

  * `ServiceCollectionExtensions` for unified setup across messaging providers.

---

## **Version Information**

* **Current Version:** v2.3.0
* Part of the private **Franz Framework** ecosystem.
---

## **Dependencies**

This package depends on:

* **Franz.Common.Http** — HTTP utilities and middleware integration.
* **Franz.Common.Messaging** — Core messaging abstractions.
* **Franz.Common.Messaging.Kafka** — Kafka backend support.
* **Franz.Common.Messaging.RabbitMQ** — RabbitMQ backend support.

---

## **Installation**

### From Private Azure Feed

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

Install the package:

```bash
dotnet add package Franz.Common.Http.Messaging
```

---

## **Usage**

### **1. Unified Messaging Context**

Register a messaging provider directly in your API:

```csharp
using Franz.Common.Http.Messaging.Extensions;

builder.Services.AddMessagingInHttpContext(builder.Configuration);
```

This single call:

* Registers the selected protocol (Kafka or RabbitMQ).
* Adds messaging transaction filters.
* Adds health checks automatically.

Your `appsettings.json` can define the provider:

```json
{
  "Messaging": {
    "Provider": "rabbitmq" // or "kafka"
  }
}
```

---

### **2. Transaction Management**

Ensures transactional consistency in request lifecycles:

```csharp
using Franz.Common.Http.Messaging.Transactions;

services.AddControllers(options =>
{
    options.Filters.Add<MessagingTransactionFilter>();
});
```

Automatically commits or rolls back messaging operations based on action result success or failure.

---

### **3. Health Checks**

Health check registration is automatic but can also be manual:

```csharp
services.AddHealthChecks()
    .AddRabbitMQ(sp => sp.GetRequiredService<IConnectionProvider>().Current);
```

All checks are tagged under `messaging` for centralized monitoring.

---

## **Integration with Franz Framework**

Seamlessly integrates with:

* **Franz.Common.Http** — Core HTTP abstractions.
* **Franz.Common.Messaging** — Foundation for all messaging systems.
* **Franz.Common.Messaging.Kafka** & **Franz.Common.Messaging.RabbitMQ** — Provider-specific implementations.

Together, they form a **poly-protocol messaging layer** for distributed microservices.

---

## **Contributing**

This package is private to the Franz Framework.
To contribute:

1. Clone the repository:
   `https://github.com/bestacio89/Franz.Common/`
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

Licensed under the **MIT License**.
See the `LICENSE` file for more details.

---

## **Changelog**

### **Version 1.6.17 — Unified Messaging Orchestration & RabbitMQ Integration**

* Added **RabbitMQ messaging integration** with health checks and scoped transaction filters.
* Introduced **`MessagingTransactionFilter`** (replacing `TransactionFilter`) for consistent messaging commit/rollback.
* Implemented unified setup via `AddMessagingInHttpContext()` for both Kafka and RabbitMQ.
* Improved health check registration to prevent duplicate service registration.
* Aligned with Kafka’s API naming convention for consistency (`AddKafkaMessaging*`).
* Synchronized versioning with the Franz Messaging ecosystem (`Kafka`, `RabbitMQ`, `AzureEventBus`).

### **Version 1.3**

* Upgraded to **.NET 9.0.8**.
* Introduced new features and compatibility with both custom Mediator and MediatR.

### **Version 1.2.65**

* Initial upgrade to **.NET 9** baseline.

---

### Version 1.6.20
- Updated to **.NET 10.0**

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

The package now uses the .NET 10 ASP.NET Core shared framework instead of legacy ASP.NET Core 2.3.11 packages, and cryptography dependencies are pinned to patched releases. Kafka, RabbitMQ, and health-check integration behavior must be validated by the solution test suite before publishing. See [the repository dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
