# 📈 Changelog

---

### Version 1.2.65
- 🌐 Kafka bootstrapping: producers, consumers, distributed messaging.
- 🗄️ Multiple database bootstrapping.
- 🧩 Multi-database support (SQL).
- 🗃️ NoSQL support.

---

### Version 1.3.1
- 🏷️ **Multi-Tenancy Enhancements**
  - Canonical `TenantResolutionResult` (with `Succeeded`, `TenantInfo`, `Source`, `Message`).
  - Added `TenantResolutionSource.Property` for message property–based resolution.
  - Refactored HTTP resolvers to use canonical models.
  - Refactored Messaging resolvers to resolve against `TenantInfo` via `ITenantStore`.
  - Implemented **DefaultTenantResolutionPipeline** and **DefaultDomainResolutionPipeline** for HTTP and Messaging.
  - Added **middleware** for automatic resolution.
  - Extended `Message` with a `Properties` dictionary and safe accessors.

- 🧵 **Mediator**
  - Initial release of **Franz.Common.Mediator**.
  - Core Dispatcher, Commands, Queries, Notifications.
  - Basic Pipelines (Logging, Validation).
  - EF integration with `DbContextBase`.
  - Observability hooks (`MediatorContext`, `IMediatorObserver`).
  - Console observer for testing/demo.
  - Optional telemetry support (tracing, correlation).

- 🔍 **Diagnostics**
  - Structured results for better logging and observability.

- ⚖️ **Consistency**
  - HTTP and Messaging now share the same contracts and patterns.

---

### Version 1.3.2
- ❌ Introduced **Error abstraction** with standard codes (`NotFound`, `Validation`, `Conflict`, `Unexpected`).
- 🔄 Extended `Result<T>` to integrate seamlessly with `Error`.
- 🛠️ Added `ResultExtensions` for ergonomic `.ToFailure<T>()` and `.ToResult()` conversions.

---

### Version 1.3.3
- 🧾 Refined **Validation pipeline** with FluentValidation adapter.
- 🔄 Improved **Transaction pipeline** with options support (rollback rules).
- 🐛 Fixed **streaming dispatcher yields** with observability.

---

### Version 1.3.4
- 🔥 Removed **AutoMapper coupling** from the framework.
- 🧹 Object mapping responsibility moved to the **Application layer**.
- ⚡ Framework remains reflection-free, adapter-friendly, with lighter dependencies.

---

### Version 1.3.5
- 🛠️ Fixed open generic pipeline registration (no more `object, object` hack).
- ✅ Transaction & caching pipelines now DI-friendly with proper constructors.
- 🔄 Resilience pipelines (Retry, Timeout, CircuitBreaker, Bulkhead) fully configurable via `FranzMediatorOptions`.
- 📘 README updated with pipeline usage and configuration examples.
- 🚀 Smoother onboarding: one-line `AddFranzMediator()` setup with options delegate.

---

### Version 1.3.6 (Franz.Common)
- 🧵 **Mediator Core**
  - Removed MediatR dependency completely.
  - Notifications and handlers now use `Franz.Mediator` (`INotification`, `INotificationHandler<>`, `IDispatcher`).
  - `IIntegrationEvent` now inherits from `INotification` → seamless pipeline + messaging integration.

- 📡 **Messaging (Kafka)**
  - `MessagingPublisher` updated to use `_dispatcher.PublishAsync()` so integration events flow through mediator pipelines before publishing to Kafka.
  - Publish method signature changed from `void` → `Task` for proper async/await.
  - `MessagingInitializer` updated to scan for `Franz.Mediator.Handlers.INotificationHandler<>`.
  - Detects event types implementing `IIntegrationEvent` and ensures topics are initialized accordingly.
  - Dead-letter & subscription topics creation streamlined with Franz’s naming conventions.

- 🔧 **Dependency Injection**
  - All DI extensions isolated into `Franz.Common.DependencyInjection.Extensions`.
  - MS.DI now just an adapter — core libraries are DI-free.
  - Franz works without DI, adapters exist for convenience.

- 🏗️ **Framework Integrity**
  - Minimal rewiring outside of DI + Messaging.
  - Only 3 classes changed (`MessagingPublisher`, `MessagingInitializer`, DI extensions).
  - Domain events, pipelines, processors, and observers unchanged.

---

### Version 1.3.9 – Database Stability Fixes
- 🐛 Fixed incorrect default port fallback (MariaDB → 3306, Postgres → 5432, SQL Server → 1433, Oracle → 1521).
- 🌐 Connection string builder now uses `127.0.0.1` instead of `localhost` to avoid socket/TCP mismatches.
- 🔒 Proper `SslMode=None` applied by default to avoid SSL negotiation issues.
- 🕵️ Masked passwords in logs for safe diagnostics.

---

### Version 1.3.10 – Scoped DbContext & Lifecycle
- 🔄 Enforced DbContext resolution through DI scope (no more “phantom DB” issues).
- ⚙️ Corrected `EnsureCreated` vs `Migrate` lifecycle usage:
  - Dev/Test → `EnsureDeleted + EnsureCreated`.
  - Prod → `Migrate()` only.
- 🛠️ Added options to configure drop/create/migrate via `DatabaseOptions`.

---

### Version 1.3.11 – Seed & Lifecycle Cleanup
- 🐛 Fixed duplicate seed issues caused by mixing `EnsureCreated` + `Migrate`.
- 📖 Clarified seeding strategy:
  - Use `HasData` only once (migrations path).
  - For dev/test, prefer manual or conditional seeding.
- 🌍 Introduced environment-aware DB lifecycle defaults (no accidental reseeds).

---

### Version 1.3.12 – Verbose Logging & Observability
- 📝 Added `LoggingPreProcessor` and `LoggingPostProcessor` with runtime request type detection.
- 🔖 Prefixed logs with `[Command]`, `[Query]`, `[Request]` for clear business-level observability.
- 🔄 Unified logging across pipelines (no more `ICommand\`1` or `IQuery\`1` names).
- 👀 Verbose lifecycle tracing: Pre → Pipeline → Post with request names.
- ⚡ Focus on Commands/Queries, not raw SQL noise.

---

### Version 1.3.14 – Correlation ID Enhancements
- 🔗 Unified correlation pipeline: IDs flow consistently across requests, notifications, pipelines.
- 📡 Automatic propagation: every log entry (request, DB query, notification, response) carries the same correlation ID.
- 🌍 External ID support: accepts incoming `X-Correlation-ID` headers.
- 🏛️ Centralized handling in **Franz.Common.Logging** for reuse/consistency.
- 🧵 Scoped logging via `ILogger.BeginScope` and Serilog’s `LogContext`.
- ⚙️ Environment-aware output: verbose payload logs in dev, correlation-focused logs in production.

---

## Version 1.4.0 – The Observability & Resilience Release 🚀

### ✨ New Modules
- **Franz.Common.Mediator.Polly**
  - Resilience pipelines: Retry, CircuitBreaker, Timeout, Bulkhead.
  - Policies resolved by name from a shared registry.
  - Unified enriched Serilog logging (correlation ID, request type, policy name, elapsed time).
  - Opt-in registration with simple extension methods.

- **Franz.Common.Caching**
  - Providers: Memory, Distributed, Redis.
  - Flexible key strategies (default, namespaced).
  - Mediator caching pipeline with automatic HIT/MISS detection.
  - Integrated observability: Serilog logs + OpenTelemetry metrics.
  - Settings cache for app flags and long-lived config values.

- **Franz.Common.Mediator.OpenTelemetry**
  - Automatic root span creation for every Mediator request.
  - Enriched with Franz tags: correlation ID, tenant, environment, pipeline.
  - Error tagging with exception details.
  - Lightweight by design — Franz produces signals, host app configures exporters.
- **Franz.Common.Http.Refit**  
  - Support: optional, config-driven Refit client registration via the HTTP bootstrapper.
  - Enable in appsettings: `Franz:HttpClients:EnableRefit = true`.
  - Register typed Refit clients automatically from config (base URL, optional policy, interface type).
  - Works with shared Polly policy registry, correlation/tenant header injection, optional token provider, Serilog + OTEL annotations.


### ⚙️ Framework Improvements
- Unified logging model across pipelines.
- Reduced boilerplate with highly opinionated bootstrappers for Database and Messaging providers.
- Clearer DX: resilience, caching, and tracing are now one-liners to enable.

Version 1.4.1 – Patch & Documentation

📚 Documentation refinements across Business and EntityFramework packages.

🛠 Minor fixes in bootstrapper examples (registration alignment).

🔧 Adjusted resilience configuration snippets in README for clarity.

✅ Internal consistency updates across subpackages (no breaking changes).

Version 1.4.2 – Cleanup & Consolidation

🗑️ Removed obsolete code:

SaveEntitiesAsync → fully merged into SaveChangesAsync (audit + domain events handled automatically).

DbContextMultiDatabase → deleted in favor of DbContextBase.

🧹 Handlers updated to use SaveChangesAsync directly.

🔄 Tightened alignment between Business and EntityFramework packages.

📚 README + CHANGELOG updates to reflect simplified context model.