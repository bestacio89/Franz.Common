# 📈 Changelog

---
### Version 1.2.65
* Everything related to kafka 
* Distributed Messaging bootstrapping
* Multiple Database Bootstrapping
* MultiDatabase Support
* NoSql Support

### Version 1.3.1

* **Multi-Tenancy Enhancements**
  * Canonical `TenantResolutionResult` (with `Succeeded`, `TenantInfo`, `Source`, `Message`).
  * Added `TenantResolutionSource.Property` for message property–based resolution.
  * Refactored all HTTP resolvers to use canonical models.
  * Refactored all Messaging resolvers to resolve against `TenantInfo` via `ITenantStore`.
  * Implemented **DefaultTenantResolutionPipeline** and **DefaultDomainResolutionPipeline** for HTTP and Messaging.
  * Added **middleware** for automatic resolution.
  * Extended `Message` with a `Properties` dictionary and safe accessors.

* **Mediator**
  * Initial release of **Franz.Common.Mediator**.
  * Core Dispatcher, Commands, Queries, Notifications.
  * Basic Pipelines (Logging, Validation).
  * EF integration with `DbContextBase`.
  * Added Observability hooks (`MediatorContext`, `IMediatorObserver`).
  * Console observer provided for testing/demo.
  * Support for optional telemetry (tracing/correlation).

* **Diagnostics**
  * Structured results for better logging and observability.

* **Consistency**
  * HTTP and Messaging now share the same contracts and patterns.

---

### Version 1.3.2
* Introduced **Error abstraction** with `Error` class and standard codes (`NotFound`, `Validation`, `Conflict`, `Unexpected`).
* Extended `Result<T>` to integrate seamlessly with `Error`.
* Added `ResultExtensions` for ergonomic `.ToFailure<T>()` and `.ToResult()` conversions.

---

### Version 1.3.3
* Refined **Validation pipeline** with FluentValidation adapter.
* Improved **Transaction pipeline** with options support (rollback rules).
* Bugfixes: ensured **streaming dispatcher yields** with observability.

---

### Version 1.3.4
* 🔥 Removed **AutoMapper coupling** from the framework.
* Responsibility for object mapping now belongs to the **Application layer**.
* Framework remains reflection-free and adapter-friendly.
* Cleaner separation of concerns, lighter dependencies, more flexible.

###  Version 1.3.5
* Fixed open generic pipeline registration (no more object,object hack).
* Transaction & caching pipelines now DI-friendly with proper constructors.
* Resilience pipelines (Retry, Timeout, CircuitBreaker, Bulkhead) fully configurable via FranzMediatorOptions.
* README updated with pipeline usage and configuration examples.
* Smoother onboarding: one-line AddFranzMediator() setup with options delegate.

###  Franz.Common v1.3.6
🔹 Mediator Core

Removed MediatR dependency completely.

All notifications and handlers now use Franz.Mediator (INotification, INotificationHandler<>, IDispatcher).

IIntegrationEvent now inherits from INotification → seamless pipeline + messaging integration.

🔹 Messaging (Kafka)

MessagingPublisher updated:

Now uses _dispatcher.PublishAsync() to process integration events through mediator pipelines before publishing to Kafka.

Publish method signature changed from void → Task for proper async/await handling.

MessagingInitializer updated:

Scans for Franz.Mediator.Handlers.INotificationHandler<> instead of MediatR handlers.

Detects all event types implementing IIntegrationEvent and ensures topics are initialized accordingly.

Dead-letter & subscription topics creation logic streamlined with Franz’s naming conventions (ExchangeNamer, HeaderNamer).

🔹 Dependency Injection

All DI extensions isolated into Franz.Common.DependencyInjection.Extensions.

MS.DI is now just an adapter — core libraries are DI-free.

Clear separation: Franz works without DI, adapters exist for convenience.

🔹 Framework Integrity

Minimal rewiring outside of DI + Messaging:

Only 3 main classes required changes (MessagingPublisher, MessagingInitializer, DI extensions).

All domain events, pipelines, processors, and observers remain unchanged — proving Franz’s abstractions were clean.

### v1.3.9 – Database Stability Fixes

Fixed incorrect default port fallback (3308 → now correct defaults per provider: MariaDB 3306, Postgres 5432, SQL Server 1433, Oracle 1521).

Connection string builder now uses 127.0.0.1 instead of localhost to avoid socket/TCP mismatches.

Proper SslMode=None applied by default to avoid unwanted SSL negotiation failures.

Masked passwords in logs for safe diagnostics.

### v1.3.10 – Scoped DbContext & Lifecycle

Enforced DbContext resolution through DI scope, preventing “phantom DB” issues.

Corrected EnsureCreated vs Migrate lifecycle usage:

Dev/Test → EnsureDeleted + EnsureCreated

Prod → Migrate() only

Added options to configure drop/create/migrate behavior via DatabaseOptions.

### v1.3.11 – Seed & Lifecycle Cleanup

Fixed duplicate seed issues caused by mixing EnsureCreated + Migrate.

Clarified seeding strategy:

Use HasData only once (migrations path).

For dev/test, prefer manual or conditional seeding.

Introduced environment-aware DB lifecycle defaults (no more accidental reseeds).

### v1.3.12 – Verbose Logging & Observability

Added LoggingPreProcessor and LoggingPostProcessor with runtime request type detection.

Prefixed logs with [Command], [Query], [Request] for clear business-level observability.

Unified logging across pipelines → no more generic ICommand\1orIQuery`1` names.

Lightweight verbose logs:

Pre → Pipeline → Post lifecycle traced with request names.

Keeps focus on Commands/Queries, not raw SQL noise.

### v1.3.14 Correlaiton Id enhancements

Unified correlation pipeline — correlation IDs now flow consistently across requests, notifications, and mediator pipelines.

Automatic propagation — every log entry (request, DB query, notification, response) carries the same correlation ID.

External ID support — accepts incoming X-Correlation-ID headers for distributed tracing across services.

Centralized handling — correlation ID logic moved into Franz.Common.Logging for reuse and consistency.

Scoped logging — integrated with ILogger.BeginScope and Serilog’s LogContext to enrich all logs automatically.

Environment-aware output — detailed payload logs in development, structured correlation-focused logs in production.
