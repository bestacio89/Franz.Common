# 📈 Changelog

---

## Version 1.2.65 – Foundation 🏗️

### ✨ Added

* 🌐 Kafka bootstrapping: producers, consumers, distributed messaging.
* 🗄️ Multi-database bootstrapping (SQL).
* 🗃️ NoSQL support.

---

## Version 1.3.1 – Multi-Tenancy & Mediator 🚦

### ✨ Added

* **Multi-Tenancy**

  * Canonical `TenantResolutionResult` (`Succeeded`, `TenantInfo`, `Source`, `Message`).
  * `TenantResolutionSource.Property` for property-based resolution.
  * Default tenant/domain resolution pipelines (HTTP + Messaging).
  * Middleware for automatic tenant/domain resolution.

* **Mediator (Initial Release)**

  * Core dispatcher for Commands, Queries, Notifications.
  * Pipelines: Logging, Validation.
  * EF integration via `DbContextBase`.
  * Observability hooks (`MediatorContext`, `IMediatorObserver`).
  * Console observer for testing.

### 🔧 Changed

* Refactored HTTP & Messaging resolvers to canonical models.

### 📚 Docs

* Structured results for better logging & observability.

---

## Version 1.3.2 – Error Model ❌

### ✨ Added

* `Error` abstraction with standard codes (`NotFound`, `Validation`, `Conflict`, `Unexpected`).
* Extended `Result<T>` to integrate seamlessly with `Error`.
* `ResultExtensions` for ergonomic conversions.

---

## Version 1.3.3 – Validation & Transactions ⚖️

### ✨ Added

* FluentValidation adapter for validation pipeline.
* Transaction pipeline with rollback rules via options.

### 🐛 Fixed

* Streaming dispatcher yields with observability.

---

## Version 1.3.4 – Decoupling AutoMapper 🔌

### 🔧 Changed

* Removed AutoMapper coupling → mapping pushed to Application layer.
* Framework remains reflection-free & adapter-friendly.

---

## Version 1.3.5 – Resilience Pipelines 🛡️

### ✨ Added

* Retry, Timeout, CircuitBreaker, Bulkhead resilience pipelines.
* Configurable caching pipelines (Memory, Distributed, Redis).

### 🐛 Fixed

* Open generic pipeline registration errors.

### 📚 Docs

* Added configuration examples for pipelines.

---

## Version 1.3.6 – Mediator Independence 🧵

### ✨ Added

* Removed MediatR dependency → now fully `Franz.Mediator`.
* `IIntegrationEvent : INotification` for clean event flow.
* `IDispatcher.PublishAsync` powers events.

### 📡 Messaging

* Kafka publisher uses `_dispatcher.PublishAsync()` for event fan-out.

### 🔧 Changed

* DI extensions isolated in `Franz.Common.DependencyInjection.Extensions`.
* Core libs DI-free, adapters optional.

---

## Version 1.3.9 – Database Stability 🐛

### 🐛 Fixed

* Default port fallback for MariaDB, Postgres, SQL Server, Oracle.
* Replaced `localhost` → `127.0.0.1` for TCP consistency.
* Default `SslMode=None`.
* Masked passwords in logs.

---

## Version 1.3.10 – Scoped DbContext 🔄

### 🔧 Changed

* Enforced DbContext resolution via DI scope.
* Corrected `EnsureCreated` vs `Migrate` usage.

---

## Version 1.3.11 – Seed Lifecycle Cleanup 🌱

### 🐛 Fixed

* Duplicate seed issues resolved.

### 🔧 Changed

* Environment-aware defaults for migrations.
* Clarified seeding strategy.

---

## Version 1.3.12 – Observability 📖

### ✨ Added

* `LoggingPreProcessor` & `LoggingPostProcessor`.
* Prefixed logs with `[Command]`, `[Query]`, `[Request]`.

---

## Version 1.3.14 – Correlation IDs 🔗

### ✨ Added

* Correlation ID flow across requests, DB, pipelines.
* Support for external IDs via `X-Correlation-ID`.

### 🔧 Changed

* Scoped logging with Serilog + ILogger.
* Environment-aware logs (Dev = verbose, Prod = lean).

---

## Version 1.4.0 – Observability & Resilience 🚀

### ✨ Added

* **Mediator.Polly** → Retry, CircuitBreaker, Timeout, Bulkhead.
* **Caching** → Memory, Distributed, Redis.
* **Mediator.OpenTelemetry** → Automatic spans with Franz tags.
* **Http.Refit** → Config-driven typed clients with Polly, correlation headers, Serilog, OTEL.

### 🔧 Changed

* Unified logging model.
* Reduced boilerplate with bootstrappers.

---

## Version 1.4.1 – Patch & Docs 📚

### 📚 Docs

* Documentation refinements.

### 🐛 Fixed

* Minor bootstrapper fixes.

---

## Version 1.4.2 – Cleanup & Consolidation 🧹

### 🔧 Changed

* Removed `SaveEntitiesAsync` → merged into `SaveChangesAsync`.
* Removed obsolete `DbContextMultiDatabase`.
* Business + EF packages aligned.

---

## Version 1.4.4 – Stability 🔥

### 🔧 Changed

* Improved logging + hybrid config.
* Cleaner DI registration.

### ✨ Added

* Elastic APM opt-in.

---

## Version 1.4.5 – Event Semantics 🐛

### 🐛 Fixed

* **Business** → `AggregateRoot` enforces `INotification`.
* **EntityFramework** → Events dispatched via `PublishAsync`.
* **Mediator** → Split `SendAsync` (commands/queries) vs `PublishAsync` (events).
* **Messaging.Kafka** → Dispatcher uses `PublishAsync`.

---

## Version 1.5.0 – Aras Integration ✨

### ✨ Added

* Completed **Aras integration** with simplified abstractions.
* Integration events → pure notifications (fan-out).

### 🔧 Changed

* Clearer semantics between Commands, Queries, Domain Events, Integration Events.
* Kafka + Hosting unified on `PublishAsync`.

---

## Version 1.5.1 – Mapping Arrives 🚀

### ✨ Added

* `Franz.Common.Mapping` as a Franz-native AutoMapper alternative.
* Profiles (`FranzMapProfile`) with `CreateMap`, `ForMember`, `Ignore`.
* By-name default mapping.
* DI support with `services.AddFranzMapping(...)`.

### 🔧 Changed

* Ecosystem consistency → mapping without external dependencies.

---

## Version 1.5.2 – Reverse Mapping 🔄

### 🐛 Fixed

* Corrected `ReverseMap()` implementation.
* Mapping storage simplified with string-based resolution.

---

## Version 1.5.4 – 1.5.8 – Maintenance 🔧

### 🔧 Changed

* Dependencies updated.
* Normalized nullability across bootstrap, messaging, Kafka.
* Async-safe `MessagingSender`.
* Cleaner `ServiceCollectionExtensions` with fail-fast guards.
* Consistent DDD exceptions (`NotFoundException`, `TechnicalException`).

### 📚 Docs

* README + docs cleanup.

### 🐛 Fixed

* Kafka consumer fail-fast on invalid payloads.
* Structured exception logging.

---

## Version 1.5.9 – Mapping Improvements 🗺️

### ✨ Added

* `AddFranzMapping` overload with assembly scanning.

### 🔧 Changed

* Cleaner DI integration for mapping registration.

---

## Version 1.5.10 – Unified Identity & SSO 🔑

### ✨ Added

* **Franz.Common.Identity**

  * `FranzIdentityContext` (UserId, Email, FullName, Roles, TenantId, DomainId).
  * `IIdentityContextAccessor` + `FakeIdentityContextAccessor`.

* **Franz.Common.Http.Identity**

  * ASP.NET Core `IdentityContextAccessor`.
  * `AddHttpIdentityContext()` DI extension.
  * Config-driven providers: WS-Fed, SAML2, OIDC, Keycloak.

* **Franz.Common.SSO**

  * `FranzSsoSettings` for unified config.
  * `AddFranzSsoIdentity()` bootstrapper.
  * JWT bearer support for APIs.
  * Claims normalization pipeline.
  * Structured logging via `FranzSsoStartupFilter`.

### 🔧 Changed

* Removed legacy `GenericSSOManager`/`GenericSSOProvider`.
* ASP.NET-specific code separated into `Franz.Common.Http.Identity`.
* Provider enforcement: one interactive provider unless allowed.

### 🐛 Fixed

* Startup errors with multiple providers.
* Normalized claims across WS-Fed/SAML2/OIDC/Keycloak.

### 📚 Docs

* Updated READMEs for Identity, Http.Identity, SSO.
* Unified usage examples with `appsettings.json` + DI extensions.

---
