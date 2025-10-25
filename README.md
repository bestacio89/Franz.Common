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
* **Franz.Common.Messaging.Kafka** → Kafka hosted services & DI bootstrap.
* **Franz.Common.Messaging.RabbitMQ** → RabbitMQ producer, consumer, and transaction orchestration.
* **Franz.Common.Http.Messaging** → Messaging + transaction filters & health checks for ASP.NET Core.
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
```

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

**Latest Version:** 1.6.19

---

## Version 1.6.18- 1.6.19 - Mapping Refinements

### 🧠 **Constructor-Aware Mapping Engine**

* Detects and invokes **record positional constructors** automatically.
* Eliminates the need for `public MemberDto() { }`.
* Allows **immutable DTOs and record structs** out-of-the-box.
* Falls back to `Activator.CreateInstance()` only when no usable constructor exists.
* 100 % backward-compatible with `ConstructUsing()` and legacy mappings.

### 🧩 **Architectural Impact**

* Strengthens immutability and contract integrity in the Franz ecosystem.
* Enables the “DTOs must be immutable” Tribunal rule to pass naturally.
* Outperforms AutoMapper in instantiation efficiency and architectural compliance.

### **1.6.17 — Messaging Orchestration & Consistency Update**

A unified release focused on **naming standardization**, **protocol-specific clarity**, and **cross-package consistency** across **Kafka**, **RabbitMQ**, and **HTTP Messaging**.

#### 🧩 Unified Messaging API

* All messaging extensions now use **explicit naming**:

  * `AddKafka*` for Kafka
  * `AddRabbitMQ*` for RabbitMQ
  * `AddMessagingInHttpContext()` for unified HTTP integration
* Improves readability, autocompletion, and architectural intent.

#### 🐇 RabbitMQ Integration

* Added **RabbitMQ support** inside `Franz.Common.Http.Messaging`.
* Introduced **`MessagingTransactionFilter`** for scoped commit/rollback logic.
* Added **RabbitMQ health checks** and intelligent duplicate prevention.
* Unified service registration and DI conventions with Kafka.

#### ☕ Kafka API Refactor

* Renamed all extension methods to the `AddKafka*` family for clarity.
* Aligned dependency registration with RabbitMQ for full parity.

#### 🌐 Ecosystem Synchronization

* Version alignment across **Kafka**, **RabbitMQ**, and **Http.Messaging**.
* Foundation laid for **AzureEventBus** support in 1.7.x.

---

➡️ Full history available in [changelog.md](changelog.md).

---

🔥 With `Franz.Common`, you can bootstrap a Kafka-ready, resilient, **polyglot microservice** with **one line of code**.

---

