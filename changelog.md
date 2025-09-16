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
