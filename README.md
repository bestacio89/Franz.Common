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

See [controbuting.md](contributing.md).

---

## 📜 License

Licensed under the **MIT License**.

---

# 🆕 Franz Framework 1.4.x → 1.6.x

### **The Observability, Identity & Polyglot Era**

---

## 📌 Changelog

**Latest Version:** `1.6.2`

---
## Franz.Framework v1.6.2

* ✨ Added AddFranzResilience(IConfiguration) — single entrypoint to configure Retry, Timeout, Bulkhead, CircuitBreaker policies from appsettings.json.

* ♻️ Internal cleanup: unified PollyPolicyRegistryOptions + Mediator pipelines under one bootstrapper.

* 🛡️ Config-driven resilience now out-of-the-box: no more manual policy registration.

* ✅ Requires Microsoft.Extensions.Configuration.Binder (for GetValue<T>() binding).

* 📦 Backward-compatible: existing AddFranzPollyRetry, AddFranzPollyTimeout, etc. still available if you need fine-grained control.

### 🚀 Version 1.6.1 – Polyglot Database & Messaging Bootstrap 🌍

✨ **Added**

* **Polyglot Persistence**

  * Extended `AddDatabase<TDbContext>` to support **MongoDB** and **Azure Cosmos DB** in addition to relational providers.
  * New `AddDatabases<TDbContext>` method enables **polyglot mode** → configure both Relational + Document stores in the same microservice.
  * Unified configuration style (`Databases:Relational` + `Databases:Document`) for clean, multi-db bootstrapping.

* **Messaging**

  * `AddMessageStore` bootstrapper now supports **MongoDB** and **Azure Cosmos DB** outbox/dead-letter stores.
  * EF-based relational stores simplified to use **generic repositories** directly (no extra boilerplate).
  * Consistent abstraction via `IMessageStore` for Mongo + Cosmos backends.

* **Cosmos Integration**

  * Added `CosmosDBMessageStore` implementation.
  * Used `PatchItemAsync` for atomic updates (`SentOn` field), aligning with Mongo `Builders.Update.Set`.
  * Added bootstrapper for Cosmos message persistence.

🔧 **Changed**

* Clean separation between **base projects** (low-level infra) and **bootstrappers** (developer-facing DI).
* APIs now rely on **bootstrappers only** (`Franz.Common.Http.EntityFramework`, `Franz.Common.MongoDB`, `Franz.Common.AzureCosmosDB`, etc.), enforcing clean architecture from the start.
* Documentation/tagline updates to reflect Franz philosophy:

  * *“Your architecture, your way — under Franz conventions (we still make the rules).”*

📚 **Docs**

* Updated **Franz.Common.Http.EntityFramework** README with NoSQL support (Mongo, Cosmos).
* Updated **Franz.Common.Messaging.EntityFramework** README with polyglot config examples.
* Polished **Franz.Template** intro with developer-friendly tagline:

  * *“Surf the architecture microservice setup with Franz.Template — clean, done right, and properly strapped on. But one thing: we don’t like boonies.”*

---

### 🚀 Version 1.6.0 – Identity, Messaging & Hosting Unification

✨ **Added**

* **Identity & SSO**
  • FranzIdentityContext with unified user/tenant/domain model.
  • HttpContextIdentityContextAccessor & DI bootstrap.
  • FranzSsoSettings with WS-Fed, SAML2, OIDC, Keycloak support.
  • JWT bearer token integration.
  • Claims normalization pipeline.

* **Messaging**
  • Outbox pattern with retries + dead-letter queue.
  • Inbox pattern for idempotent consumers.
  • IMessageSerializer abstraction with JSON default implementation.
  • KafkaHostedService & OutboxHostedService for hosted consumption/dispatch.
  • DI extensions for Mongo outbox and Kafka hosting.
  • Async IListener interface with cancellation support.
  • Structured emoji logging and OpenTelemetry hooks.

🔧 **Changed**

* Removed legacy GenericSSOManager/EF Identity coupling.
* Refactored message DTOs to decouple mediator from transports.
* Extracted ASP.NET Core specifics into `Franz.Common.Http.Identity`.
* Enforced: only one interactive SSO provider active at a time.

🐛 **Fixed**

* Startup issues with multiple SSO providers.
* Serialization mismatches between Kafka & Outbox.
* Claims normalization consistency across all SSO providers.

📚 **Docs**

* Updated READMEs for **Messaging**, **MongoDB**, **Hosting**, **Hosting.Kafka**, **Identity**, **Http.Identity**, **SSO**.
* Added usage guides for provider configuration & DI extensions.

---

### Version 1.5.9 – Mapping Improvements ⚡

* By-name fallback mapping (zero config).
* Profiles with `CreateMap`, `ForMember`, `Ignore`, `ReverseMap`, `ConstructUsing`.
* Expression-based mapping with caching.
* DI integration with `AddFranzMapping`.
* NEW in 1.5.6 → Assembly scanning for auto-registration of profiles.

### Version 1.5.4 - 1.5.8 – Maintenance Nullability Cleanup 🧹

* Updated dependencies.
* Documentation cleanup & upgrades.
* Removed redundant `Business.HandlerCollector`.
* Normalized nullability across bootstrap, messaging, Kafka.
* Refactored `MessagingSender` to async-safe.
* Structured logging improvements.
* Fail-fast guards in DI.
* Kafka consumer → strict payload validation.
* Consistent DDD exception usage.

### Version 1.5.2 – Reverse Mapping Unlocked 🔄

* Fixed `ReverseMap()` to correctly generate reverse mappings.
* Replaced expression storage with string-based property resolution.
* Convention-based mapping fallback.

### Older Versions

* **1.5.1** – Native Mapping Arrives
* **1.5.0** – When Aras Becomes Simple
* **1.4.5** – Patch Release: Event Semantics
* **1.4.4** – Logging improvements, hybrid config, Elastic APM opt-in, perf boosts.
* **1.4.2** – Removed `SaveEntitiesAsync`; cleaned multi-db DbContext.
* **1.4.0** – Migrated to C# 12, resilience pipelines, observability.

➡️ Full history available in [changelog.md](changelog.md).

---

🔥 With `Franz.Common`, you can bootstrap a Kafka-ready, resilient, **polyglot microservice** with **one line of code**.

```
