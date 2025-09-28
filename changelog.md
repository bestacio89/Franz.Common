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

✨ Added

Introduced Outbox pattern with StoredMessage DTO and persistence mappings.

Implemented IMessageStore abstraction for persistence-agnostic outbox storage.

Added Inbox pattern (IInboxStore) for idempotent message consumption.

Introduced IMessageSerializer abstraction with JSON default implementation.

Added correlation tracking via MessageContextAccessor.

🔧 Changed

Refactored transport-level Message to decouple mediator from messaging.

Standardized serialization across Kafka and Mongo outbox.

Improved structured logging with emoji conventions (✅ success, ⚠️ retry, 🔥 DLQ).

🐛 Fixed

Serialization mismatches between Kafka and Outbox replays.

Missing correlation propagation in consumer pipelines.

📚 Docs

Updated README to document Outbox, Inbox, retry/DLQ, serializer abstraction, and monitoring hooks.

📦 Franz.Common.MongoDB
[Release] v1.5.10 – Outbox Persistence & Indexing 🍃

✨ Added

MongoMessageStore implementation of IMessageStore.

MoveToDeadLetterAsync to handle exhausted retries.

Automatic index creation on SentOn, RetryCount, and CreatedOn.

DI extension: AddMongoMessageStore().

🔧 Changed

Clean separation of persistence DTOs (StoredMessage) from runtime transport (Message).

📚 Docs

Updated README to show Mongo outbox/inbox usage, retry/DLQ behavior, and DI setup.

📦 Franz.Common.Messaging.Hosting
[Release] v1.5.10 – Async Listeners & Context 🎧

✨ Added

Async IListener interface (Listen(CancellationToken)).

MessageContextAccessor.Set/Clear for safe context management.

Inbox checks in listeners for idempotent dispatch.

🔧 Changed

Refactored OutboxMessageListener & KafkaMessageListener to async pattern.

Unified message deserialization pipeline.

📚 Docs

Updated README with v1.5.10 features and changelog.

📦 Franz.Common.Messaging.Hosting.Kafka
[Release] v1.5.10 – Kafka Hosting Bridge ☕

✨ Added

KafkaHostedService to run Kafka listeners as hosted services.

OutboxHostedService to publish Mongo outbox messages into Kafka.

DI extension: KafkaHostingServiceCollectionExtensions with AddKafkaHostedListener() & AddOutboxHostedListener().

🔧 Changed

Separated transport (Messaging.Kafka) from orchestration (Hosting.Kafka).

📚 Docs

Added new README with usage, DI setup, logging conventions, and changelog.

📦 Franz.Common.Identity
[Release] v1.5.10 – Unified Identity 🔑

✨ Added

FranzIdentityContext (UserId, Email, FullName, Roles, TenantId, DomainId).

IIdentityContextAccessor & FakeIdentityContextAccessor for testing.

📦 Franz.Common.Http.Identity
[Release] v1.5.10 – HTTP Identity Accessor 🌐

✨ Added

HttpContextIdentityContextAccessor (ASP.NET Core).

DI extension: AddHttpIdentityContext().

Config-driven providers: WS-Fed, SAML2, OIDC, Keycloak.

Automatic claims normalization into FranzIdentityContext.

📦 Franz.Common.SSO
[Release] v1.5.10 – Unified SSO 🚪

✨ Added

FranzSsoSettings for unified configuration in appsettings.json.

One-line bootstrap: AddFranzSsoIdentity().

JWT bearer token support.

Unified claims normalization pipeline across providers.

Startup logging via FranzSsoStartupFilter.

🔧 Changed

Removed legacy GenericSSOManager & EF Identity coupling.

Extracted ASP.NET Core specifics into Franz.Common.Http.Identity.

Enforced: only one interactive provider active unless explicitly allowed.

🐛 Fixed

Startup issues from multiple provider registrations.

Claims normalization across WS-Fed, SAML2, OIDC, and Keycloak.

📚 Docs

Updated READMEs for Identity, Http.Identity, and SSO with provider setup examples.

---
