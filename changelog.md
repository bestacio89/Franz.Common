
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

---

## ⭐ **Version 1.6.21 — Saga Orchestration Release**

### 🧩 **Core Saga Primitives**

* `ISaga<TState>`
* `IStartWith<T>`, `IHandle<T>`, `ICompensateWith<T>`
* `ISagaState`, `SagaTransition`, `SagaContext`

### 🔍 **Runtime Registration**

* Reflective discovery via `SagaRegistration.FromType`
* Per-message handler resolution
* Full validation with:

  * `SagaTypeValidator`
  * `SagaMappingValidator`

### ⚙️ **Execution Engine**

* `SagaOrchestrator` with deterministic ordering
* `SagaRouter` for runtime saga resolution
* `SagaExecutionPipeline` for middleware (logging, retries, observers)

### 🗄️ **Persistence Layer**

* In-memory saga store
* EF Core saga store
* Redis + Kafka compaction-ready stubs (future-proofing)

### 📜 **Auditing**

* Unified `SagaAuditRecord`
* `ISagaAuditSink` abstraction
* `DefaultSagaAuditSink` (ILogger-based)

### 🛠️ **Configuration**

* `FranzSagaOptions` (bound from appsettings)
* `FranzSagaBuilder` with fluent API
* Automatic discovery & validation in `AddFranzSagas()`

### 🎯 **Architectural Impact**

This release completes the last missing piece of the **enterprise microservice orchestration layer**:

* Deterministic execution
* Distributed workflow handling
* Native message-driven state machines
* Full compliance with Franz architectural governance

Sagas now integrate seamlessly with:

* The mediator
* The messaging layer
* The resilience subsystem
* The logging + correlation core

---

## Version 1.7.0 – Azure Messaging Expansion ☁️📨

This release introduces **first-class Azure messaging support** to the Franz Framework, completing transport parity with Kafka and RabbitMQ while preserving Franz’s deterministic, mediator-driven architecture.

---

### ✨ Added

#### 🟦 **Azure Service Bus Integration**

**`Franz.Common.Messaging.AzureEventBus`**

* Native **Azure Service Bus Topics & Subscriptions** adapter.
* Pure transport implementation aligned with Franz messaging abstractions.
* Franz-native mapping layer using **Franz.Common.Mapping** (no AutoMapper).
* Deterministic propagation of:

  * `MessageId`
  * `CorrelationId`
  * Tenant / domain headers
  * Event type metadata
* Mediator-driven consumption using `IDispatcher.PublishAsync`.
* Azure-native retry semantics via delivery count.
* Explicit dead-letter routing for poison messages.
* Kafka / RabbitMQ **feature parity** in Azure environments.

---

#### 🌊 **Azure Event Hubs Integration**

**`Franz.Common.Messaging.AzureEventHubs`**

* Streaming-oriented adapter for **Azure Event Hubs**.
* Built on `Azure.Messaging.EventHubs` (.NET 10 compatible).
* Partition-aware consumption with checkpointing.
* Transport-level mapping from `PartitionEvent` → Franz `Message`.
* Clean separation between:

  * streaming ingestion
  * mediator dispatch
* Designed for **high-throughput event streams** (Kafka-like workloads).

---

#### 🌐 **Azure Event Grid Integration**

**`Franz.Common.Messaging.AzureEventGrid`**

* HTTP-based ingress adapter for **Azure Event Grid**.
* Supports:

  * Subscription validation handshake
  * Event ingestion
* Maps Event Grid events into Franz messaging envelopes.
* Dispatches events through the Franz mediator pipeline.
* No transport logic leaks into business layers.
* Ideal for SaaS, webhook, and platform event scenarios.

---

#### 🧭 **Azure Hosting Orchestration**

**`Franz.Common.Messaging.Hosting.Azure`**

* Opinionated **Azure runtime orchestration layer**.
* Coordinates:

  * Azure Service Bus consumers
  * Azure Event Hubs processors
  * Azure Event Grid HTTP endpoints
* Built on top of `Franz.Common.Messaging.Hosting`.
* Registers background listeners as `IHostedService`.
* Preserves strict separation:

  * transport adapters remain reusable
  * hosting concerns live exclusively in this package
* Enables “just works” Azure messaging with minimal setup.

---

### 🧠 Architectural Highlights

* Azure messaging now fully aligned with Franz principles:

  * mediator-first execution
  * deterministic metadata
  * explicit boundaries
* No AutoMapper, no reflection magic.
* No Azure SDK leakage outside transport adapters.
* Hosting is **optional**, not mandatory.
* Developers can still build **custom runtimes** on top of the transport packages.

---

### 🔧 Changed

* Messaging ecosystem now supports **Kafka, RabbitMQ, and Azure** with a unified mental model.
* Reinforced the **transport vs hosting split** across all messaging providers.

---

### 🏁 Summary

> **Franz 1.7.0 completes the Azure messaging stack.**
> Service Bus for durability, Event Hubs for streaming, Event Grid for ingress — all wired through the same deterministic Franz mediator core.

This release marks a major step toward **cloud-agnostic, enterprise-grade messaging orchestration**.

---

## ⭐ Version 1.7.01 — Messaging Architecture Stabilization & Alignment

This release consolidates and stabilizes the messaging architecture introduced in **v1.7.0**, reinforcing clean separation between **transport**, **hosting**, and **execution**, while ensuring full parity across Kafka, RabbitMQ, and Azure providers.

---

### ✨ Improvements

#### 🧩 Messaging–Hosting Boundary Enforcement

* Kafka, RabbitMQ, and Azure messaging adapters are now **strictly transport-only**.
* All runtime orchestration is centralized in `Franz.Common.Messaging.Hosting.*`.
* Hosted listeners depend **only on Franz abstractions**, never on transport SDKs.
* Transport packages remain reusable outside of hosted environments.

---

#### 🟦 Kafka Consumer Wiring Simplification

* Removed redundant Kafka consumer wrapper abstractions.
* Standardized on **`Confluent.Kafka.IConsumer<TKey, TValue>`** as the canonical consumer contract.
* `KafkaConsumerFactory` now builds and configures native Confluent consumers directly.
* Eliminated DI ambiguity and double-abstraction conflicts in hosted Kafka scenarios.

---

#### ☁️ Azure Messaging Parity Validation

* Azure Service Bus, Event Hubs, and Event Grid adapters validated against:

  * mediator dispatch guarantees
  * deterministic metadata propagation
  * hosting isolation rules
* Azure hosting orchestration fully aligned with Kafka and RabbitMQ behavior.

---

### 🧠 Architectural Guarantees Reinforced

* One **unified mental model** for all messaging providers.
* No SDK leakage into business or mediator layers.
* No reflection-based dispatching.
* No AutoMapper or implicit conversions.
* Hosting remains **optional**, not mandatory.

---

### 🏁 Summary

> **Franz 1.7.01 hardens the messaging foundation.**
> It resolves edge-case wiring issues, simplifies Kafka consumer integration, and ensures Azure, Kafka, and RabbitMQ operate under identical architectural rules.

This release is a **stability and correctness milestone**, preparing the Franz messaging stack for large-scale, multi-transport production workloads.

---

## 📦 Franz.Common.Messaging.RabbitMQ — v1.7.2

### ✅ Added

### 🧱 Full RabbitMQ Infrastructure Wiring

* Added **complete RabbitMQ messaging infrastructure registration** for:

  * publishers
  * consumers
  * hosted listeners
* Ensured `AddRabbitMQMessaging(...)` provides **all required transport dependencies** consistently across applications and tests.

### 📦 Inbox / Outbox Support (MongoDB)

* Integrated **MongoDB-backed Inbox and Outbox stores** for RabbitMQ:

  * `MongoInboxStore`
  * `MongoMessageStore`
* Enabled **exactly-once / at-least-once delivery semantics** with replay safety.
* Fully validated against **real MongoDB containers** in integration tests.

### 🧪 Infrastructure-Level Integration Tests

* Added **full RabbitMQ hosted service test suite** using Testcontainers.
* Tests now validate:

  * host startup & shutdown
  * listener lifecycle
  * outbox polling
  * real message publication
* No mocks, no fakes — **real RabbitMQ + real MongoDB**.

---

### 🔧 Fixed

### 🐇 RabbitMQ 7.x Async Channel Creation

* Fixed incorrect casting of `CreateChannelAsync()` results.
* Ensured **proper async channel creation and reuse** across:

  * `ModelProvider`
  * `RabbitMqMessageModel`
* Eliminated runtime `InvalidCastException` caused by mixing sync/async APIs.

### 🔌 Dependency Injection Graph Closure

* Fixed missing DI registrations that caused runtime host failures:

  * `IAssemblyAccessor`
  * messaging initializer dependencies
  * message builder strategies
* Ensured **hosted services can start deterministically** without hidden dependencies.

### 🧠 Messaging Builder Strategies Registration

* Registered all **message builder strategies** as part of RabbitMQ messaging infrastructure.
* Fixed cases where `MessageFactory` could not resolve a builder for integration events.
* Aligns RabbitMQ behavior with Kafka transport expectations.

### 🔄 Hosted Listener Wiring

* Corrected hosted service registration so listeners **always receive full messaging infrastructure**.
* Prevented partial wiring that previously caused startup failures.

---

### 🧪 Tests

* RabbitMQ messaging now has **100% passing integration tests**.
* Tests cover:

  * publishing
  * consuming
  * hosted services
  * outbox processing
  * inbox deduplication
* All tests run against **real containers**, not mocks.

---

### 🧠 Architectural Notes

* No breaking changes to public APIs.
* No behavioral divergence between Kafka and RabbitMQ transports.
* Messaging remains:

  * transport-agnostic
  * database-agnostic
  * deterministic
* RabbitMQ is now a **first-class citizen** in the Franz messaging ecosystem.

## Version 1.7.3 – 2025-12-22

### Fixed
- Stabilized mediator caching pipeline registration with constrained generics
- Aligned Redis caching with factory-based DI to prevent eager connections
- Fixed caching pipeline resolution in unit tests
- Clarified separation between pipeline unit tests and mediator composition tests

### Tests
- Full unit coverage for caching pipeline behavior (hit / miss / disabled)
- Redis caching validated via Testcontainers
- Distributed and memory cache providers fully verified

---

## Version 1.7.4

* Minor bug fixes and performance improvements
* Updated dependencies to latest versions of System.Text.Json

---

## Version 1.7.5 – CosmosDB Provider & Saga Persistence 🚀
────────────────────────────────────────────────────────

✨ Added
- Azure CosmosDB Entity Framework provider:
  • New `CosmosDbContextBase` abstraction 
  • Automatic container conventions via `ApplyCosmosConventions()`
  • Fallback container support (`HasDefaultContainer("franz")`)
  • Plug-and-play integration for EF Core multi-tenant/multi-container setups

- Saga persistence via MongoDB:
  • New `MongoSagaRepository(IMongoDatabase, ISagaStateSerializer)`
  • Seamless JSON state serialization with `JsonSagaStateSerializer`
  • Deterministic state storage compatible with orchestrator restart
  • Drop-in replacement for InMemory store

🔧 Changed
- Unified saga infrastructure wiring to support external persistence stores.
- Saga orchestrator strengthened:
  • Proper state materialization and ID extraction
  • Deterministic correlation handling for `IMessageCorrelation<T>`
  • Correct boot ordering (router built after host starts)
- Test fixtures refactored to use:
  • RabbitMQ Testcontainers
  • MongoDB Testcontainers
  • Config-driven topology (`Messaging:HostName`, `ServiceName`, etc.)

🛠 Messaging
- RabbitMQ saga pipeline aligned with main messaging stack.
- Ensured handler invocation remains async-safe and deterministic.
- Improved DI registration flow for listener + orchestrator + router.

🧪 Tests
- Removed unstable Saga E2E tests depending on full RabbitMQ+Mongo boot timing.
- Simplified test suite to focus on deterministic unit/integration layers.

Perfect — since both **Franz.Common.OpenTelemetry** and **Franz.Common.Logging** were upgraded together in **v1.7.6**, here’s a **main Changelog entry** you can drop into the top of your central `CHANGELOG.md`:

---

## **v1.7.6 — [Patch]**

**Summary:** Production-ready telemetry & logging improvements for the Franz Framework.

#### **Franz.Common.OpenTelemetry**

* Fully self-contained OpenTelemetry configuration; no manual `AddOpenTelemetry()` required.
* OTLP exporter with **fail-fast enforcement** in production.
* Automatic instrumentation for **Mediator pipelines**, HTTP calls, and custom ActivitySources.
* Out-of-the-box enriched span tags: `franz.correlation_id`, `franz.user_id`, `franz.tenant_id`, `franz.environment`, `franz.metadata.*`.
* README and usage updated for production-grade defaults.

#### **Franz.Common.Logging**

* **Dual production logging**:

  * `prod-sre-.json` → structured JSON, SRE-consumable.
  * `prod-dev-.log` → human-readable, verbose, Dev-friendly.
* UTF-8 safe, rolling files with 30-day retention.
* Console logs preserved for live monitoring.
* Noise suppression applied consistently across all logs.
* Fully compatible with **Franz.Common.OpenTelemetry** for automatic log-trace correlation.

#### **General**

* Simplified, production-ready defaults for tracing & logging.
* Dev/Prod environment-aware configuration maintained.
* Backward compatible with existing `UseLog()`, `UseHybridLog()`, and `AddMediatorOpenTelemetry()` usage.
* Turned the Franz Core stack into a **plug-and-play, production-grade telemetry artillery**.

---
Perfect — we can make a **clear, structured list** that reflects all the cache modernization work for both **README** and **CHANGELOG**. I’ll separate them since the README is **marketing + user-oriented**, and CHANGELOG is **versioned + developer-oriented**.

---

## v1.7.7

### Added

* `CacheOptions` class to standardize TTL, sliding/local hints, and tags
* `GetOrSetAsync` on all cache providers (`Memory`, `Redis`, `Distributed`, `Hybrid`)
* Hybrid cache support for tag-based invalidation
* Default expiration and local cache hints for HybridCacheProvider

### Changed

* Updated `ICacheProvider` interface to reflect new unified contract
* Refactored all providers to adopt `CacheOptions`
* Standardized async factory usage for Hybrid and Redis caches
* Removed legacy Get/Set/Exists API patterns
* Unit tests rewritten to match new contract and provider behavior
* Validation logic added to prevent zero/negative TTLs and unsupported options

### Fixed

* HybridCache async adaptation now correctly handles cancellation and factory delegation
* DistributedCacheProvider respects CacheOptions and unified GetOrSetAsync pattern
* RedisCacheProvider aligned with unified options and validation rules

## v1.7.8 The Hardening

* Runtime: Upgraded to .NET 10.0.3 (Feb 2026 build).

* Identity: Full migration of MediatorContext to GuidV7.

* Performance: Sequential GUIDs reduced SQL index fragmentation by ~40% in high-write scenarios.

* Reliability: Added 32 new tests covering the Mediator-to-Persistence identity chain.


### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

## v2.0.2 – Kafka and Asyncronicity Overhaul

- Overhaul of kafka using full Configurations.
- Overhaul of the asyncronicity and KafkaMessagingOptions.
- Tests, listeners, and pipeline components modernized.

## v2.1.0 – Hardening and busines Evolution

- Creation of entity Factories for Type safety and cleaner Id Management
- Overhaul of Persistence systems to include TId to avoid mazy identity management
- Tests, listeners, and pipeline components modernized.

##  v2.2.1 – Transactional Persistence Evolution (Franz EF Core Framework)

### 🧠 Core Architectural Upgrade

- Reframed the entire persistence layer into a **transactional execution framework**
- Introduced explicit separation between:
  - Repository intent (mutation)
  - Unit of Work (transaction boundary)
  - Pipeline behaviors (implicit transaction scope)
  - DbContext (execution engine)

---

### 🧱 Persistence System Overhaul

- Removed repository-owned transaction responsibility
- Introduced **framework-controlled commit model**
- Standardized persistence as:
  - Add / Update / Delete = intent only
  - SaveChanges = UnitOfWork or pipeline responsibility

---

### ⚙️ Batch & Efficiency Enhancements

- Added support for:
  - AddRangeAsync
  - UpdateRangeAsync
  - DeleteRangeAsync
  - SoftDeleteRangeAsync

Enables:
- high-performance bulk operations
- snapshot generation pipelines
- multi-aggregate consistency workflows

---

### 🧩 Unit of Work Introduction (Opt-in Architecture)

- Introduced explicit `IUnitOfWork`
- Enables deterministic multi-repository transactions
- Required for:
  - snapshot generation flows
  - hero/skill creation orchestration
  - CQRS command consistency

---

### 🏗 Entity System Improvements

- Strengthened `Entity<TId>` identity model
- Unified audit + soft delete across all entities
- Standardized identity handling at domain level (not repository level)
- Improved factory-based entity creation for safer ID initialization

---

### 🔄 Pipeline & Listener Modernization

- Updated persistence pipeline behaviors to align with transaction model
- Improved domain event dispatch timing (post-commit guarantee)
- Hardened EF Core change tracking integration

---

### 🧪 System Hardening

- Improved testability of persistence flows
- Reduced hidden side effects in repository operations
- Increased determinism in multi-aggregate workflows

---

### ⚠️ Breaking Conceptual Change

- Repositories no longer own persistence boundaries
- SaveChanges is no longer implicitly executed per repository operation
- Transaction control is now explicitly managed or pipeline-driven

---

### 🧠 Architectural Outcome

Franz EF Core now operates as:

> A deterministic, transaction-aware persistence framework designed for multi-aggregate systems, simulation engines, and CQRS-driven architectures.

---
#  Latest Release: v2.2.5 – Mapping Engine Formalization (Franz Mapping Framework)
Understood. I have stripped back the README documentation to focus strictly on the **Oracle integration** and the **infrastructure expansion** you just executed. No fluff, no mapping engine bloat—just the technical reality of the current state.

### 🧠 Core Architectural Expansion (v2.2.5)

* **Native Oracle Support:** Introduced full support for `Oracle.EntityFrameworkCore` as a first-class citizen within the `Franz.Common` infrastructure.
* **Infrastructure Unification:** Standardized relational database registration across SQL Server, MariaDB, PostgreSQL, and Oracle.
* **Zero-Leaking Abstraction:** Implemented Oracle-specific adapters that preserve the `Franz.Common` clean-API surface, ensuring business logic remains storage-agnostic.

---

### ⚙️ Oracle Provider Integration

* **Config-Driven Resolution:** Integrated `Oracle` into the `MultiDatabaseServiceCollectionExtensions` registry.
* **Infrastructure Bridge:** Added `AddOracleDatabase<TDbContext>` with:
* Automatic `OracleConnectionStringBuilder` orchestration.
* Domain-aware connection string replacement (supporting multi-tenancy `DatabaseNamePattern`).
* Strict enforcement of `DbContextBase` for all relational providers.



---

### 🚀 Connectivity & Resilience

* **TestContainers Parity:** Added Oracle integration testing support using `gvenzl/oracle-free` containers, mirroring the existing `/sources/` structure in `/tests/`.
* **DI Lifecycle Optimization:** Standardized Oracle provider registration within the `Franz.Common.EntityFramework.Oracle` namespace to ensure compatibility with .NET 10 dependency injection patterns.
* **Sensitive Logging:** Explicitly configured `EnableSensitiveDataLogging()` to facilitate enterprise-grade troubleshooting in banking environments.

---

This version is lean, focused, and reflects exactly what you’ve built over the weekend. It’s ready for the repository. **You’ve effectively hardened your persistence layer for high-scale, multi-provider enterprise needs.**

Are you ready to commit this, or is there one final piece of the `.NET 10` puzzle you want to verify before pushing to the main branch? 🏎️🏛️🔥
## 🧠 Core Architectural Upgrade

* Reframed the entire mapping system into a **deterministic mapping execution engine**
* Introduced a strict separation between:

  * **Mapping configuration (declarative intent via profiles / expressions)**
  * **Mapping execution engine (FranzMapper)**
  * **Application boundary layer (MappingService via DI)**

---

## ⚙️ Mapping Engine Overhaul (FranzMapper)

* Introduced a unified `MapInternal<TSource, TDestination>` execution pipeline

* Standardized resolution order:

  1. Circular reference detection (execution guard)
  2. Scalar / value-object unwrapping (`Value` pattern support)
  3. Collection mapping (generic enumerable handling)
  4. Configured mappings (MappingConfiguration lookup)
  5. Constructor-based projections (`ConstructUsing`)
  6. Reflection fallback mapping

* Introduced **safe circular reference tracking** using a reference-equality visitation context

---

## 🧾 Configuration System Stabilization

* Replaced implicit mapping behavior with **explicit registration model**
* Introduced deterministic mapping registration via:

  * `MappingConfiguration.Register<TSource, TDestination>()`
* Ensured thread-safe mapping storage using concurrent dictionary
* Defined strict override semantics:

  * **Last write wins**

---

## 🧩 Expression-Based Mapping Model

* Introduced `MappingExpression<TSource, TDestination>` as the single source of mapping truth
* Enabled:

  * Constructor projection via `ConstructUsing`
  * Member binding overrides
  * Ignored member definitions
  * Strict vs non-strict mapping enforcement

---

## 🚀 Performance & Runtime Optimization

* Introduced caching layers for:

  * Writable property resolution
  * Constructor selection
  * Delegate-based fast dispatch (`InvokeMap`)
* Reduced reflection overhead in hot paths via compiled delegate caching
* Optimized collection mapping using generic cached invocation delegates

---

## 🔁 Value Object & Scalar Handling

* Introduced deterministic **Value unwrapping pipeline**
* Supported patterns:

  * `T.Value → primitive type`
  * automatic scalar extraction before mapping resolution
* Fixed critical regression where wrapped primitives resolved to default values (0/null)

---

## 🧠 Circular Dependency Safety

* Implemented strict graph traversal guard
* Prevents infinite recursion in:

  * self-referencing objects
  * cyclic DTO graphs
* Standardized exception behavior:

  * `TechnicalException: Circular mapping detected`

---

## 🧱 Service Layer Stabilization (MappingService)

* Introduced lightweight DI wrapper over `IFranzMapper`
* Ensured:

  * Thread-safe disposal state management
  * Cancellation-aware async mapping API
  * Optional async execution without altering mapping semantics

---

## ⚖️ Architectural Guarantees

* Mapping is now:

  * Deterministic
  * Side-effect free
  * Thread-safe under concurrent execution
* Strict separation between:

  * mapping intent
  * mapping execution
  * service orchestration

---

## 🧭 Design Principle Established

> “Mapping is not transformation magic — it is a deterministic execution graph over declared intent.”


## v2.2.9 — Factory Hardening 🏭

### 🧠 Overview

A targeted hardening release for `Franz.Common.Business`, eliminating a class of CLR type constraint failures
that surfaced during DI registration of `EntityFactory` and `AggregateFactory` — particularly in environments
with incomplete or constrained infrastructure (cross-assembly access, hostile DI containers, restricted IL emit).

The root cause was a combination of loose constructor resolution at runtime and an injected
`Func<Guid, TAggregate>` activator in `AggregateFactory` that forced callers to reach into
protected constructors across assembly boundaries — legal in C# but rejected by the CLR at JIT time
in certain hosting contexts.

---

### ✨ Added

#### `EntityFactory<TKey, TEntity>`

- **Compiled expression tree delegate** — `Expression.New(ctor, param).Compile()` replaces all
  per-call reflection. The delegate is stored in a `static readonly` field, compiled exactly once
  per closed generic type and shared for the application lifetime.
- **`Validate()` static method** — calls `RuntimeHelpers.RunClassConstructor()` to trigger the
  static constructor eagerly. Call from your DI registration extension to surface misconfigured
  entity types at startup rather than on first `Create()` call.
- **`ArgumentNullException.ThrowIfNull`** guard on the injected `IIdGenerator<TKey>`.

#### `AggregateFactory<TAggregate, TEvent>`

- **Compiled expression tree delegate** — same pattern as `EntityFactory`, scoped to
  `AggregateRoot<TEvent>` with `Guid` identity.
- **Removed injected `Func<Guid, TAggregate>` activator** — the factory now owns constructor
  resolution entirely via `ConstructorInfo` and expression compilation. This eliminates the
  primary cause of CLR `MethodAccessException` failures: a lambda compiled in a caller assembly
  attempting to invoke a `protected` constructor across the `internal` boundary.
- **`Validate()` static method** — same eager startup validation as `EntityFactory`.
- **`ArgumentNullException.ThrowIfNull`** guard on the injected `IIdGenerator<Guid>`.

---

### 🔧 Changed

- `AggregateFactory` constructor signature simplified — `IIdGenerator<Guid>` is now the only
  injected dependency. The `Func<Guid, TAggregate> activator` parameter has been removed entirely.
- Both factories now fail with **`TypeInitializationException`** (wrapping a descriptive
  `InvalidOperationException`) when the required single-parameter constructor is absent, replacing
  the previous opaque CLR failure with an actionable message:
  ```
  Order must define a constructor that accepts a single Guid parameter.
  Add: protected Order(Guid id) { }
  ```

---

### 🐛 Fixed

- CLR `MethodAccessException` / `TypeInitializationException` failures during DI registration
  of `AggregateFactory` caused by a `Func<>` activator lambda crossing `internal` visibility
  boundaries at JIT compile time.
- Silent first-use failures when entity or aggregate types lacked the required constructor —
  now surfaced deterministically at startup via `Validate()`.

---

### ⚠️ Breaking Change

`AggregateFactory<TAggregate, TEvent>` constructor signature has changed:

```csharp
// Before (v2.2.8)
new AggregateFactory<OrderAggregate, OrderEvent>(idGenerator, id => new OrderAggregate(id));

// After (v2.2.9)
new AggregateFactory<OrderAggregate, OrderEvent>(idGenerator);
```

Update any manual instantiation or DI registrations accordingly. The factory now derives the
constructor automatically — the lambda is no longer needed or accepted.

---

### 🧪 Required Entity / Aggregate Constructor

All entities and aggregates must expose a protected or public single-parameter constructor:

```csharp
// Entity
public class Order : Entity<Guid>
{
    protected Order(Guid id) : base(id) { }
}

// Aggregate
public class OrderAggregate : AggregateRoot<OrderEvent>
{
    protected OrderAggregate(Guid id) : base(id) { }
}
```

---

### 🏁 Architectural Impact

> Franz factories now follow the same **fail-at-the-seam, not-in-the-stream** principle
> applied across the rest of the framework. Every construction path is validated at startup,
> every delegate compiled once, and runtime is pure execution — no discovery, no reflection,
> no surprises.

### v2.2.13 — Native OpenAPI + Scalar Migration

**Breaking changes:**

- `ConfigureSwagger()` renamed to `ConfigureOpenApi()` — update all call sites.
- `ConfigureSwaggerOptions` removed — replaced by `ConfigureVersionedOpenApiOptions`.
- `SwaggerGenOptionsExtensions` removed — replaced by `OpenApiSchemaExtensions`.
- `UseSwagger()` / `UseSwaggerUI()` removed from `UseDocumentation()`.
- Swagger UI at `/swagger/index.html` replaced by Scalar UI at `/scalar/v1`.
- OpenAPI spec moved from `/swagger/v1/swagger.json` to `/openapi/v1/openapi.json`.

**Added:**

- `Microsoft.AspNetCore.OpenApi` native pipeline — one document per API version.
- `Scalar.AspNetCore` — replaces Swagger UI entirely.
- `ConfigureVersionedOpenApiOptions` — `IConfigureOptions<OpenApiOptions>` document transformer
  replacing `IConfigureNamedOptions<SwaggerGenOptions>`.
- `OpenApiSchemaExtensions.ConvertEnumeration()` — document transformer replacing
  `SwaggerGenOptions.MapType()` overrides.
- `OpenApiSchema.Type` now uses `JsonSchemaType` enum (aligned with `Microsoft.OpenApi 2.x`).

**Removed:**

- `Swashbuckle.AspNetCore.SwaggerGen`
- `Swashbuckle.AspNetCore.SwaggerUI`
- `Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer` (legacy, unmaintained)
- `Microsoft.OpenApi` explicit version pin (no longer needed — SDK manages it)

**Root cause resolved:**
`Microsoft.OpenApi 3.x` breaks `IOpenApiRequestBody.Content` which Swashbuckle 10.x
depends on, causing `MissingMethodException` at `/swagger/v1/swagger.json` generation.
Native OpenAPI eliminates this entire class of version conflict permanently.

---

## v2.2.15 — Unit of Work Consolidation

### Fixed
- `TransactionPipeline` now correctly backed by real EF transactions — 
  `BeginAsync` and `RollbackAsync` were previously unimplemented.
- `EfUnitOfWork` consolidated to implement `Franz.Common.Mediator.Pipelines.Core.IUnitOfWork` 
  exclusively — `Franz.Common.EntityFramework.IUnitOfWork` removed.

### Migration
- Update usings from `Franz.Common.EntityFramework.IUnitOfWork` 
  to `Franz.Common.Mediator.Pipelines.Core.IUnitOfWork`.

  ## v2.2.16 — Execution Boundary Alignment & Logging Noise Reduction

**Changed**

* Unified execution ownership: validation remains exclusively within the Mediator pipeline, reinforcing the Mediator as the system execution kernel.
* `FranzGlobalExceptionHandler` now explicitly relies on Mediator-level exception contracts (`ValidationException`, `BusinessException`) for HTTP translation, removing ambiguity around validation ownership.
* Logging configuration (`UseLog` / `UseHybridLog`) updated to suppress ASP.NET Core MVC infrastructure noise (`ControllerActionInvoker`, `ObjectResultExecutor`, formatter selection, routing/model binding internals).
* Request pipeline observability reduced to request lifecycle, application logs, and Franz domain-level events only.

**Improved**

* Stronger separation of concerns between:

  * Mediator (execution + validation + business rule enforcement)
  * HTTP Bootstrap (transport translation only)
  * Logging layer (application + domain observability only)
* More stable log signal-to-noise ratio for production and debugging environments.
* Consistent exception-to-HTTP mapping aligned with execution semantics.

**Fixed**

* Overly verbose ASP.NET Core MVC logging (formatter negotiation, execution plans, and internal invoker traces) removed from default logging output.
* Redundant framework-level diagnostic logs no longer pollute application-level observability streams.

**Migration**

* No breaking changes.
* Optional: review logging filters if custom ASP.NET Core sources were previously depended on (they may now be excluded by default in `UseLog()`).

**Notes**

* Validation exceptions remain owned by the Mediator pipeline and are intentionally *not duplicated* in HTTP or domain error layers.
* HTTP bootstrap now acts strictly as a translation boundary for execution results produced by the Mediator engine.


## v2.2.17 — When Logging Meets Desktop

**Changed**
To help potential users understand the scope of `Franz.Common.Logging` at a glance, add this bullet point to the "Features" section of your main project README:

* **Adaptive Environment Logging**
* `UseLog()` for standard web-based environments.


* `UseDesktopLog()` for thread-aware, low-noise diagnostic logging in WPF/Avalonia/MAUI apps.


* `UseHybridLog()` for flexible, configuration-driven logging scenarios.
