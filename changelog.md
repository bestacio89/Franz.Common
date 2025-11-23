
# 📈 **Franz Framework – Full Changelog**

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
* Profiles (`FranzMapProfile`) with `CreateMap`, `ForMember`, `Ignore`).
* By-name default mapping.
* DI support with `services.AddFranzMapping(...)`.

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

## Version 1.6.0 – The Consolidation Release 🏗️🔑📦

*(see previous full details — Outbox/Inbox, Identity, Domain Events, etc.)*

---

## Version 1.6.1 – Polyglot Persistence & Messaging 🌍

### ✨ Added

* Extended `AddDatabase<TDbContext>` → supports **MongoDB** and **Azure Cosmos DB**.
* New `AddDatabases<TDbContext>` for **multi-provider mode** (Relational + Document).
* Config-driven selection via `Databases:Relational` + `Databases:Document`.
* `AddMessageStore` → supports **MongoDB** and **CosmosDB** outbox/dead-letter.
* Added `CosmosDBMessageStore` implementation with atomic updates.

### 🔧 Changed

* Bootstrappers philosophy → APIs depend only on bootstrappers, not base projects.
* Clear split: base projects = infra, bootstrappers = developer entrypoints.

### 📚 Docs

* Updated `Franz.Common.Http.EntityFramework` and `Franz.Common.Messaging.EntityFramework` with NoSQL examples.
* Refined `Franz.Template` with new tagline.

---

## Version 1.6.2 – Resilience & Null Safety 🛡️

### ✨ Added

* `AddFranzResilience(IConfiguration)` → single entrypoint for Retry, Timeout, Bulkhead, CircuitBreaker.

### 🔧 Changed

* Unified `PollyPolicyRegistryOptions` + Mediator pipelines.
* Config-driven resilience now fully bootstrapped.
* Full nullability compliance (`<Nullable>enable + <TreatWarningsAsErrors>true>`).
* Generic constraints realigned (`IAggregateRootRepository<T, TEvent>` enforces `IDomainEvent`).
* Messaging & serialization hardened (safe deserialization, async-safe dispatch).

### ✨ Messaging

* Improved Kafka listeners (async-safe).
* RabbitMQ integration enhanced (TLS 1.3 only, structured logging, correlation propagation).

### 🧪 Tests

* Full integration tests validated under null-safety.

---

## Version 1.6.3 – Multi-Environment & Cosmos Governance 🌐🗄️

### ✨ Added

* **Environment-Aware Bootstrapper**

  * Detects and validates `appsettings.{Environment}.json`.
  * Enforces correct configuration per environment (Dev, Test, Prod).
* **AzureCosmosStore**

  * Introduced as a generic base for Cosmos DB persistence.
  * `AddCosmosDatabase<TStore>` extension for clean DI registration.
* **Governance Enforcement**

  * No hardcoded connection strings accepted.
  * Fail-fast validation for provider/context mismatches.
* **Multi-Database Validation**

  * Unified checks across EF, Mongo, Cosmos.
  * Clear runtime exceptions for invalid setups.

### 🔧 Changed

* Improved multi-database orchestration → cleaner separation of relational vs NoSQL contexts.
* More explicit runtime errors for invalid or missing configs.

---

## Version 1.6.4 – 1.6.14 – Chaos Benchmark Release 🌀🔥

### ✨ Added

* **Unified Franz Polly Resilience Integration**

  * Single-entry `AddFranzResilience()` for all Mediator and HTTP policies.
  * Automatic registration of Retry, CircuitBreaker, Timeout, Bulkhead.
  * Shared `PolicyRegistry` across Mediator + HTTP.
  * Observers and correlation tracking for full resilience telemetry.

* **Chaos Simulation Mode (Dev Only)**

  * Failure simulation for stress testing and resilience validation.
  * Ensures recovery, retry, and logging integrity.

* **Advanced Structured Logging**

  * Automatic injection of `FranzRequest`, `FranzCorrelationId`, and `FranzPolicy`.
  * Correlated logs across policies and Mediator pipelines.

---

### 🧩 Fixed

* Typed Policy Resolution: resolved `InvalidCastException` in Mediator pipelines.
* Corrected policy naming (`mediator:RetryPolicy`, etc.).
* Verified sequential resilience chaining: Retry → CircuitBreaker → Timeout → Bulkhead.

---

### 🧠 Improved

* Clearer debug output during policy registration.
* Chaos mode driven entirely by configuration.
* Simplified resilience JSON structure.

---

### 🧭 Example Configuration

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

### 🏁 Summary

> Franz now reaches **full deterministic resilience orchestration** — chaos tested, fully correlated, and operationally beautiful.
> All failures are intentional, observable, and instructive.

---

## Version 1.6.15 – ReadRepository Fix 🔧

* Fixed compile-time `InvalidCastException` from `List<T>` → `IQueryable<T>`.
* `GetAll()` now returns `IReadOnlyCollection<T>` for safer semantics.

---

## Version 1.6.16 – Logging Overhaul & Platform Stability 🧾

### 🔹 Highlights

* **Unified Logging Core** → consolidated all environment-aware logging into `UseLog()` and `UseHybridLog()`.
* **Noise Filtering** → EF Core, HttpClient, ASP.NET, and hosting chatter removed.
* **UTF-8 Enforcement** → strict encoding across all sinks.
* **Contextual Enrichment** → app, machine, environment metadata added.
* **Elastic APM Integration** → available in DEBUG.
* **Version Synchronization** → all 54 projects aligned under v1.6.16.

---

## Version 1.6.17 – Messaging Orchestration & Consistency Update 🧩

A unified release focusing on **messaging layer alignment**, **naming consistency**, and **protocol extensibility** across **Kafka**, **RabbitMQ**, and **HTTP-based messaging** integrations.

---

### ☕ **Franz.Common.Messaging.Kafka**

✅ **Extension Method Rebrand for Uniformity & Intent Clarity**
All Kafka registration extensions were renamed to follow the **explicit `AddKafka*` convention**, ensuring every API call clearly indicates its messaging backend.

**Updated method list:**
`AddKafkaMessaging()` • `AddKafkaMessagingPublisher()` • `AddKafkaMessagingSender()` • `AddKafkaMessagingConsumer()` • `AddKafkaMessagingConfiguration()`

🧠 **Purpose:**
To standardize naming across all Franz messaging providers and make intent instantly recognizable in dependency registration blocks.

---

### 🐇 **Franz.Common.Http.Messaging**

* Added **RabbitMQ messaging integration** with dedicated health checks and scoped transaction filters.
* Introduced **`MessagingTransactionFilter`** (replacing `TransactionFilter`) for consistent commit/rollback behavior across messaging operations.
* Implemented **unified registration** via `AddMessagingInHttpContext()` for both Kafka and RabbitMQ providers.
* Improved **health check registration** to automatically avoid duplicate service entries.
* Aligned with Kafka’s **API naming convention** for consistency (`AddKafkaMessaging*`).
* Established **version synchronization** across all Franz messaging packages (`Kafka`, `RabbitMQ`, `AzureEventBus`).

---

### 🔧 Global Notes

* Messaging layer now adheres to **protocol-specific clarity** (`Kafka`, `RabbitMQ`, `AzureEventBus`).
* Improves maintainability and onboarding clarity across all Franz microservice templates.
* Paves the way for **Franz 1.7.x** modular expansion and telemetry standardization.

---

> 🧭 **Note:**
> This changelog is intentionally detailed — serving both as a **learning artifact** for junior developers and an **audit trail** for Franz ecosystem evolution.
> Each entry reflects design reasoning, dependency evolution, and architectural refinements across the entire framework.

---

## Version 1.6.18 - 1.6.19 - Mapping Refinements

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

Absolutely — let’s **merge everything you’ve done into one clean, enterprise-ready block** for:

1. **Master README**
2. **CHANGELOG.md**

This will reflect:

* The global **1.6.20 .NET 10 modernization**
* The **RabbitMQ-specific improvements**
* The **removal of Oracle**
* The stabilized CI/CD
* The updated templates
* The updated messaging abstractions

I’ll craft BOTH blocks now.

---

# ✅ **MASTER README — Version 1.6.20 Release Section**

Paste this into the **main README** under your “Changelog” or “What’s New” section:

---




## **1.6.20 — .NET 10 Modernization Release**

### **Runtime & Platform**

* Migrated all Franz packages to **.NET 10.0**
* Improved runtime consistency and cross-package dependency alignment
* Stabilized all Azure DevOps pipelines for .NET 10

### **Messaging**

* **RabbitMQ**:

  * Updated client dependencies to latest stable version
  * Realigned abstractions for consistency with Kafka
  * Improved dependency injection patterns for hosted services
  * Unified outbox hosting and listener lifecycle
* **Kafka**:

  * No changes (already aligned with .NET 10+ architecture)

### **Databases**

* Updated SQL Server, PostgreSQL, MariaDB/Pomelo, MySQL, MongoDB, and Cosmos modules
* **Removed Oracle EFCore provider**:

  * Due to Oracle’s official provider lacking .NET 9/10 support
  * Vendor update cycle consistently lags by multiple major versions
  * Prevents microservice adoption, CI/CD modernization, and cloud-native alignment
  * Last supported Franz version with Oracle: **1.6.19**

### **Templates**

* Updated API, Messaging, and Infra templates to .NET 10 defaults
* Modernized bootstrapping (logging, mediator, messaging, OTEL)

### **Documentation**

* Updated READMEs, code samples, and high-level diagrams
* Added compatibility matrix
* Added Oracle deprecation notice
* Improved messaging examples and templates

### 🚀 **Major Features**

* Introduced full **Franz gRPC Canonical Pipeline** for both Client and Server:

  * Validation
  * Tenant Resolution
  * Authorization
  * Logging
  * Metrics
  * Exception Mapping
    Distributed evenly across client/server interceptors.

* Added **GrpcServerBehaviorProvider** and **GrpcClientBehaviorProvider**, with pipeline caching and canonical ordering.

* Implemented **GrpcCallContext** abstraction for unified call metadata:

  * CorrelationId
  * RequestId
  * TenantId
  * UserId
  * ServiceName
  * MethodName
  * Deadline
  * Cancellations

* Added **FranzGrpcClientFactory** supporting:

  * Named services (via `FranzGrpcClientOptions.Services`)
  * Auto channel creation
  * Timeout handling
  * Metadata injection
  * Optional retries (future-ready)

* Full redesign of **FranzGrpcClientOptions**, including:

  * `Services` dictionary for routing
  * Default timeouts
  * Metadata injection
  * Logging toggles
  * Retry configuration

### 🧱 **New Configuration Types**

* `FranzGrpcClientServiceConfig`
* `FranzGrpcClientOptions`
* `FranzGrpcOptions`

### 🧰 **Hosting & Context Utilities**

* `GrpcContextExtensions` for metadata extraction on server-side
* Added `Hosting/NoOp` package:

  * `NoOpValidationEngine`
  * `NoOpAuthorizationService`
  * `NoOpTenantResolver`
  * `NoOpGrpcLogger`
  * `NoOpGrpcMetrics`
    Ensuring clean boot without user-defined behaviors.

### 🔧 **Dependency Injection Improvements**

* Added `AddFranzGrpcServer`, `AddFranzGrpcClient`, and `AddFranzGrpcDefaults`
* Cleaned DI boundaries:

  * Core library no longer calls `AddGrpc()` or `AddGrpcClient()`
  * The host application owns ASP.NET Core integration
* Removed all ASP.NET Core dependencies from core package

### 🧹 **Project Structure Cleanup**

* Corrected naming of client-side interceptors (`*ClientBehavior`)
* Moved ASP.NET Core routing extensions out of core package
  (now to be included in `Franz.Common.Grpc.AspNetCore`)
* Ensured strict core/hosting separation, consistent with Franz ecosystem

### ⚡ **Performance Enhancements**

* Behavior pipelines now cached per `(TRequest, TResponse)` pair
* No-op implementations ensure zero overhead when features are unused

### 🛠️ **Refactor & Code Modernization**

* Updated to .NET 10 targets
* Simplified options binding with Microsoft.Extensions.Options
* Unified server/client behavior architecture to mirror:

  * Franz.Common.Http
  * Franz.Common.Messaging
  * Franz.Common.Mediator


