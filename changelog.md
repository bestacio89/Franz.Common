# 📈 Changelog

---

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