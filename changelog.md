Perfect 👍 — let’s unify the style across your changelog so it looks clean, professional, and consistent. I’ll make every entry follow the same format:

```
## Version X.Y.Z – Title 🎯
### ✨ Added
### 🔧 Changed
### 🐛 Fixed
### 📚 Docs
```

Here’s the cleaned-up **full changelog including 1.4.5 and 1.5.0**:

---

# 📈 Changelog

---

## Version 1.2.65 – Foundation 🏗️

* 🌐 Kafka bootstrapping: producers, consumers, distributed messaging.
* 🗄️ Multiple database bootstrapping.
* 🧩 Multi-database support (SQL).
* 🗃️ NoSQL support.

---

## Version 1.3.1 – Multi-Tenancy & Mediator 🚦

### ✨ Added

* **Multi-Tenancy Enhancements**

  * Canonical `TenantResolutionResult` with `Succeeded`, `TenantInfo`, `Source`, `Message`.
  * Added `TenantResolutionSource.Property` for message property–based resolution.
  * Implemented **DefaultTenantResolutionPipeline** & **DefaultDomainResolutionPipeline** (HTTP + Messaging).
  * Middleware for automatic tenant/domain resolution.

* **Mediator (Initial Release)**

  * Core Dispatcher, Commands, Queries, Notifications.
  * Basic Pipelines (Logging, Validation).
  * EF integration with `DbContextBase`.
  * Observability hooks (`MediatorContext`, `IMediatorObserver`).
  * Console observer for demo/testing.

### 🔧 Changed

* Refactored HTTP & Messaging resolvers to canonical models.

### 🔍 Diagnostics

* Structured results for better logging & observability.

---

## Version 1.3.2 – Error Model ❌

* Introduced **Error abstraction** with standard codes (`NotFound`, `Validation`, `Conflict`, `Unexpected`).
* Extended `Result<T>` to integrate seamlessly with `Error`.
* Added `ResultExtensions` for ergonomic conversions.

---

## Version 1.3.3 – Validation & Transactions ⚖️

* Refined **Validation pipeline** with FluentValidation adapter.
* Improved **Transaction pipeline** with rollback rules via options.
* Fixed **streaming dispatcher yields** with observability.

---

## Version 1.3.4 – Decoupling AutoMapper 🔌

* Removed AutoMapper coupling → mapping responsibility pushed to Application layer.
* Framework stays reflection-free & adapter-friendly.

---

## Version 1.3.5 – Resilience Pipelines 🛡️

* Fixed **open generic pipeline registration**.
* Transaction & caching pipelines now DI-friendly.
* Resilience pipelines (Retry, Timeout, CircuitBreaker, Bulkhead) fully configurable.
* README updated with configuration examples.
* Simplified onboarding with one-line `AddFranzMediator()`.

---

## Version 1.3.6 – Mediator Independence 🧵

### ✨ Added

* Removed MediatR dependency → now fully `Franz.Mediator`.
* `IIntegrationEvent : INotification` for clean event flow.
* `IDispatcher.PublishAsync` powers events.

### 📡 Messaging (Kafka)

* Publisher now uses `_dispatcher.PublishAsync()` → events flow through pipelines.
* Async publish (`Task` return type).
* Topic initialization streamlined.

### 🔧 DI

* Extensions isolated in `Franz.Common.DependencyInjection.Extensions`.
* Core libs DI-free, adapters optional.

---

## Version 1.3.9 – Database Stability Fixes 🐛

* Fixed default port fallback (MariaDB 3306, Postgres 5432, SQL Server 1433, Oracle 1521).
* Switched `localhost` → `127.0.0.1` for TCP consistency.
* Default `SslMode=None`.
* Masked passwords in logs.

---

## Version 1.3.10 – Scoped DbContext 🔄

* Enforced DbContext resolution via DI scope.
* Corrected `EnsureCreated` vs `Migrate` lifecycle usage.
* Options added for drop/create/migrate.

---

## Version 1.3.11 – Seed Lifecycle Cleanup 🌱

* Fixed duplicate seed issues.
* Environment-aware defaults for migrations.
* Clarified seeding strategy.

---

## Version 1.3.12 – Verbose Logging & Observability 📖

* Added `LoggingPreProcessor` & `LoggingPostProcessor`.
* Prefixed logs with `[Command]`, `[Query]`, `[Request]`.
* Unified request lifecycle logging.

---

## Version 1.3.14 – Correlation IDs 🔗

* Unified correlation flow across requests, DB, pipelines.
* Accepts external IDs via `X-Correlation-ID`.
* Scoped logging with Serilog + ILogger.
* Environment-aware logs (Dev = verbose, Prod = lean).

---

## Version 1.4.0 – Observability & Resilience 🚀

### ✨ New Modules

* **Mediator.Polly** → Retry, CircuitBreaker, Timeout, Bulkhead.
* **Caching** → Memory, Distributed, Redis.
* **Mediator.OpenTelemetry** → automatic spans with Franz tags.
* **Http.Refit** → Config-driven typed clients with Polly, correlation headers, Serilog, OTEL.

### ⚙️ Improvements

* Unified logging model.
* Reduced boilerplate with bootstrappers.
* DX improvements for resilience/caching/tracing.

---

## Version 1.4.1 – Patch & Docs 📚

* Documentation refinements.
* Minor bootstrapper fixes.
* Adjusted resilience config snippets.
* Internal consistency updates.

---

## Version 1.4.2 – Cleanup & Consolidation 🧹

* Removed `SaveEntitiesAsync` → merged into `SaveChangesAsync`.
* Removed obsolete `DbContextMultiDatabase`.
* Aligned Business + EntityFramework packages.
* Docs/README updated.

---

## Version 1.4.4 – Stability Meets Firepower 🔥

* Logging & hybrid config improvements.
* Cleaner DI registration.
* Elastic APM opt-in.
* Performance boosts in mediator pipelines.

---

## Version 1.4.5 – Event Semantics Fix 🐛

### 🐛 Fixed

* **Business** → `AggregateRoot` enforces `INotification`; `GetUncommittedChanges()` returns `IReadOnlyCollection<BaseDomainEvent>`.
* **EntityFramework** → Domain events dispatched via `PublishAsync` instead of `Send`.
* **Mediator** → Split `SendAsync` (commands/queries) vs `PublishAsync` (events).
* **Messaging.Hosting.Mediator** → Integration events published via `PublishAsync`.
* **Messaging.Kafka** → Kafka dispatcher uses `PublishAsync`.

### ✅ Outcome

* Clear separation restored:

  * Commands = intentions.
  * Queries = retrieval.
  * Events = notifications (fan-out, no return).

---

## Version 1.5.0 – When Aras Becomes Simple ✨

### ✨ Added

* Completed **Aras integration** with simplified abstractions.
* Full alignment across Business, EF, Mediator, Messaging.
* Integration events now **pure notifications** (fan-out).
* Kafka + Hosting layers unified on `PublishAsync`.

### 🔧 Changed

* Clearer semantics between **Commands, Queries, Domain Events, Integration Events**.
* Stronger enforcement of **publish/notify** for events across the stack.

### 🚀 Foundation

* Prepared for **polyglot messaging** in upcoming 1.5.x releases.

Version 1.5.1 – AutoMapper++ Arrives 🚀
✨ Added

Introduced Franz.Common.Mapping as a lightweight, Franz-native alternative to AutoMapper.

Support for profiles (FranzMapProfile) with CreateMap, ForMember, and Ignore.

Default by-name mapping when no explicit profile exists.

Dependency injection support with services.AddFranzMapping(...).

Tested and validated in the Book API integration project to ensure real-world readiness.

🔧 Improved

Ecosystem consistency — Franz now provides mapping without external dependencies.

Framework is closer to being a self-contained, end-to-end enterprise stack.
## Version 1.5.2 – Reverse Mapping Unlocked 🔄

### 🔧 Fixed
- Corrected `ReverseMap()` to properly generate reverse mappings.  
- Switched mapping storage to string-based property resolution for simplicity.  
- Updated ApplyMapping to use reflection for property assignment.  
- Ensured fallback to convention-based mapping when no explicit rule is defined.  

---## Version 1.5.4 - Maintenance
* Dependencies updated

---
