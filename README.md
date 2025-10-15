# **Franz.Common**

**Franz.Common** is the heart of the **Franz Framework** — a lightweight, modular framework that streamlines the development of **event-driven microservices**.  
It was born to reduce boilerplate and architectural complexity in modern .NET systems, with a **Kafka-first** design, but extensible to **RabbitMQ, Azure Service Bus, Redis, and HTTP APIs**.

Franz provides **DDD + CQRS building blocks**, **resilience pipelines**, **auditing**, and **multi-tenancy** support across HTTP and messaging layers — batteries included, but modular.

---

## 📦 Subpackages

Franz is modular: install only what you need.

* **Franz.Common.Business** → DDD + CQRS abstractions, domain events, resilience pipelines.
* **Franz.Common.EntityFramework** → DbContextBase with auditing, soft deletes, domain event dispatching.
* **Franz.Common.Mediator** → Lightweight Mediator with pipelines for caching, logging, validation, resilience.
* **Franz.Common.Http.Bootstrap** → ASP.NET Core bootstrapper (DI, config, pipelines).
* **Franz.Common.Http.Refit** → Refit integration with Polly, logging, tenant/correlation headers.
* **Franz.Common.Logging** → Correlation ID propagation + structured logging with Serilog.
* **Franz.Common.MultiTenancy** → Tenant/domain resolution across HTTP and messaging.
* **Franz.Common.Errors** → Unified error handling models.
* **Franz.Common.Messaging** → Messaging abstractions with outbox, inbox, retry/DLQ, serializer.
* **Franz.Common.Messaging.Hosting** → Async listener orchestration & context management.
* **Franz.Common.Messaging.Hosting.Kafka** → Kafka hosted services & DI bootstrap.
* **Franz.Common.MongoDB** → Mongo-based outbox/inbox stores with retries and dead letter.
* **Franz.Common.AzureCosmosDB** → CosmosDB outbox/inbox stores with retries and dead letter.
* **Franz.Common.Identity** → Unified identity context.
* **Franz.Common.Http.Identity** → HttpContext-based identity accessor & providers.
* **Franz.Common.SSO** → Unified SSO configuration with WS-Fed, SAML2, OIDC, Keycloak.

---

## 🚀 Why Franz?

Franz doesn’t reinvent the wheel. It builds on proven ideas from **MediatR** and **Polly**, but **extends them into a cohesive framework** for modern microservices.

* ✅ **Pipelines included** → Logging, validation, caching, transactions, resilience.
* ✅ **Environment-aware observability** → verbose in dev, lean in prod.
* ✅ **Multi-database adapters** → Postgres, MariaDB, SQL Server, Oracle, Mongo, Cosmos.
* ✅ **Messaging first-class** → Kafka out-of-the-box, extensible with Mongo/Cosmos outbox.
* ✅ **Lean core, optional add-ons** → nothing hidden, integrations are opt-in.

Think of Franz as **Spring Boot for .NET microservices** — a batteries-included starter kit.

---

## 🛠 Getting Started

### Installation

Add the core library:

```bash
dotnet add package Franz.Common --version 1.6.1
````

Or install subpackages (e.g., `Business` + `EntityFramework`):

```bash
dotnet add package Franz.Common.Business --version 1.6.1
dotnet add package Franz.Common.EntityFramework --version 1.6.1
```

### Software Dependencies

* **.NET 9+**
* **Kafka 2.6+** (or RabbitMQ/Azure Service Bus with adapters)
* **Confluent.Kafka** client (for Kafka transport)
* **Docker** (for integration testing)

---

## ⚙️ Core Features

* **Domain-Driven Design (DDD) building blocks** → Entities, Aggregates, Domain Events.
* **CQRS-ready mediator pipelines** → Logging, Validation, Polly, OpenTelemetry, Transactions.
* **Polyglot persistence** → Config-driven EF Core, MongoDB, or CosmosDB bootstrappers.
* **Messaging outbox/inbox** → Retry, dead-letter queue, idempotency.
* **Resilience pipelines** → Polly retries, circuit breakers, caching, fallback policies.
* **Multi-tenancy** → Tenant-aware services and request correlation across HTTP and messaging.
* **Observability baked-in** → Logging with correlation IDs, OpenTelemetry hooks, structured Serilog sinks.

---

## 🧪 Build & Test

```bash
git clone https://github.com/bestacio89/Franz.Common.git
cd Franz.Common
dotnet build
dotnet test
```

Integration tests with Kafka:

```bash
docker-compose up -d
dotnet test --filter Category=Integration
```

---

## 🤝 Contributing

Contributions are welcome (internal team preferred).

1. Clone repo.
2. Create a feature branch (`feature/<desc>`).
3. Submit PR.
4. Add tests + docs.

See [contributing.md](contributing.md).

---

## 📜 License

Licensed under the **MIT License**.

---

# 🆕 Franz Framework 1.4.x → 1.6.x

### **The Observability, Identity & Polyglot Era**

---


## 📌 **Changelog**

**Latest Version:** 1.6.14

---

🚀 **Version 1.6.4 – 1.6.14 Chaos Benchmark Release 🌀🔥**

### ✨ Added

* **Unified Franz Polly Resilience Integration**

  * `AddFranzResilience()` → single entrypoint to register Retry, CircuitBreaker, Timeout, and Bulkhead policies.
  * Shared `PolicyRegistry` for both Mediator and HTTP pipelines.
  * Full observability via correlation ID and resilience observers.

* **Chaos Simulation Mode (Development Only)**

  * JSON-driven chaos testing for resilience validation.
  * Simulated failures:

    * `🍌 Banana Republic Exception: simulated DB meltdown!`
    * `☕ Just a friendly reminder to take a break!`
  * Ensures recovery logic and retry mechanisms function under controlled failure.

* **Advanced Structured Logging**

  * Injects `FranzRequest`, `FranzCorrelationId`, `FranzPolicy`, and `FranzPipeline` into every log event.
  * Fully compatible with Elastic, Seq, and Application Insights.
  * Uniform telemetry across all pipelines.

---

### 🧩 Fixed

* Resolved `InvalidCastException` for `IAsyncPolicy<TResponse>` by standardizing typed policy registration.
* Stabilized sequential policy chaining (Retry → CircuitBreaker → Timeout → Bulkhead).
* Ensured observer notifications always propagate duration, circuit, and timeout data.

---

### 🧠 Improved

* Registry logging now enumerates every active policy at startup.
* Cleaner error context for CircuitBreaker and Timeout events.
* Chaos simulation and retry tests fully driven by `appsettings.{Environment}.json`.

---

### 🧭 Configuration Example

```json
"Resilience": {
  "RetryPolicy": { "Enabled": true, "RetryCount": 3, "RetryIntervalMilliseconds": 200 },
  "CircuitBreaker": { "Enabled": true, "FailureThreshold": 0.5, "DurationOfBreakSeconds": 30 },
  "TimeoutPolicy": { "Enabled": true, "TimeoutSeconds": 5 },
  "BulkheadPolicy": { "Enabled": true, "MaxParallelization": 10, "MaxQueueSize": 20 },
  "ChaosMode": { "Enabled": true, "FriendlyBreaks": true, "BananaFailures": true }
}
```

---

> 🧭 Franz 1.6.14 marks the full mastery of resilience orchestration —
> deterministic, chaos-tested, and operationally self-aware.
> Every failure is intentional, observable, and recorded with beauty.

---

🚀 **Version 1.6.3 – Multi-Environment & Cosmos Governance 🌐🗄️**

### ✨ Added

* **Environment-Aware Bootstrapper** → auto-detects `appsettings.{Environment}.json`, validates per-environment configuration.
* **AzureCosmosStore Base** → generic Cosmos DB persistence context mirroring EF + Mongo.
* `AddCosmosDatabase<TStore>` → clean Cosmos DI bootstrapper.
* **Governance Enforcement** → no hardcoded connection strings, fail-fast provider/context validation.
* **Multi-Database Validation** → unified checks for EF, Mongo, Cosmos.

### 🔧 Changed

* Cleaner separation between relational and NoSQL contexts.
* More explicit runtime errors for invalid or missing configurations.

---

➡️ Full history available in [changelog.md](changelog.md).

---


🔥 With `Franz.Common`, you can bootstrap a Kafka-ready, resilient, **polyglot microservice** with **one line of code**.
