# Franz.Common

**Franz.Common** is a lightweight, modular framework designed to streamline the development and maintenance of **Kafka-based microservices**. It provides common abstractions, utilities, and patterns that make building reliable, scalable, and maintainable event-driven systems simpler and more consistent across projects.

---

## Table of Contents

1. [Introduction](#introduction)
2. [Getting Started](#getting-started)

   1. [Installation](#installation)
   2. [Software Dependencies](#software-dependencies)
   3. [Latest Releases](#latest-releases)
   4. [API References](#api-references)
3. [Sub-Repositories](#sub-repositories)
4. [Usage: Multi-Tenancy](#usage-multi-tenancy)
5. [Build and Test](#build-and-test)
6. [Contribute](#contribute)
7. [License](#license)
8. [Changelog](#changelog)

---

## Introduction

**Franz.Common** was born out of a need to reduce boilerplate and complexity when working with [Apache Kafka](https://kafka.apache.org/) in microservices architectures. The project’s primary objective is to:

* Provide **common abstractions** for Kafka producers and consumers.
* Offer **shared utilities** for message serialization, logging, retries, and error handling.
* Enable a **consistent developer experience** across different microservices.
* Provide **robust multi-tenancy support** across both HTTP and Messaging layers.

Whether you’re creating a new microservice from scratch or adding Kafka support to an existing system, **Franz.Common** aims to simplify your development process by offering well-tested building blocks and patterns.

---

## Getting Started

### Installation

For a .NET environment, you can include **Franz.Common** via NuGet:

```bash
dotnet add package Franz.Common --version <latest_version>
```

### Software Dependencies

* **.NET 9+** (with support for .NET 9 in latest versions)
* **Kafka 2.6+** (earlier versions may work, but not officially supported)
* **Confluent.Kafka** client library (or any Kafka client library your environment supports)
* **Docker** (optional) for containerized local development and testing

### Latest Releases

Check the [Releases](https://github.com/your-org/franz.common/releases) section for:

* Release notes
* New features and breaking changes
* Beta or pre-release packages

### API References

Detailed API documentation is available here:

* [Franz.Common Documentation](https://github.com/your-org/franz.common/wiki)
* Generate docs locally:

  ```bash
  dotnet build
  ```

---

## Sub-Repositories

Franz.Common is part of a suite of repositories that collectively provide full coverage for Kafka-based microservices:

1. **Franz.Producer** – handles message production with batching, serialization, and retry mechanisms.
2. **Franz.Consumer** – simplifies consumer group management, message handling, and parallel processing.
3. **Franz.Utils** – provides shared utility classes, logging integrations, serialization helpers, and custom middleware.
4. **Franz.Sample** – sample microservice demonstrating real-world usage.

---

## Usage: Multi-Tenancy

Multi-tenancy support is built into both **HTTP** and **Messaging** components of the framework.

### 1. Register Services

```csharp
using Franz.Common.Http.MultiTenancy.Extensions;
using Franz.Common.Messaging.MultiTenancy.Extensions;

public void ConfigureServices(IServiceCollection services)
{
    // Core multi-tenancy
    services.AddFranzMultiTenancy();

    // HTTP resolvers and pipeline
    services.AddFranzHttpMultiTenancy();

    // Messaging resolvers and pipeline
    services.AddFranzMessagingMultiTenancy();
}
```

---

### 2. Use HTTP Multi-Tenancy

```csharp
using Franz.Common.Http.MultiTenancy.Middleware;

public void Configure(IApplicationBuilder app)
{
    // Resolve tenant/domain once per request
    app.UseFranzMultiTenancy();

    app.UseRouting();
    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
}
```

Available resolvers:

* `HostTenantResolver`
* `HeaderTenantResolver`
* `QueryStringTenantResolver`
* `JwtClaimTenantResolver`

---

### 3. Use Messaging Multi-Tenancy

```csharp
using Franz.Common.Messaging.MultiTenancy.Middleware;

public class MessagingPipeline
{
    public void Configure(IMessagePipelineBuilder pipeline)
    {
        pipeline.Use<TenantResolutionMiddleware>();
        pipeline.Use<DomainResolutionMiddleware>();
    }
}
```

Available resolvers:

* `HeaderTenantResolver`
* `MessagePropertyTenantResolver`
* `HeaderDomainResolver`
* `MessagePropertyDomainResolver`

---

### 4. Access Tenant/Domain Context

```csharp
using Franz.Common.MultiTenancy;

public class MyService
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IDomainContextAccessor _domainContextAccessor;

    public MyService(ITenantContextAccessor tenantContextAccessor, IDomainContextAccessor domainContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _domainContextAccessor = domainContextAccessor;
    }

    public void PrintContext()
    {
        var tenantId = _tenantContextAccessor.GetCurrentTenantId();
        var domainId = _domainContextAccessor.GetCurrentDomainId();

        Console.WriteLine($"Tenant: {tenantId}, Domain: {domainId}");
    }
}
```

---

## Build and Test

### Build

```bash
git clone https://github.com/your-org/franz.common.git
cd franz.common
dotnet build
```

### Test

Run all tests:

```bash
dotnet test
```

Run integration tests with Kafka (requires Docker):

```bash
docker-compose up -d
dotnet test --filter Category=Integration
```

---

## Contribute

Contributions are welcome!

1. Submit issues for bugs or feature requests.
2. Fork the repo and create feature branches (`feature/<desc>` or `bugfix/<desc>`).
3. Submit pull requests for review.
4. Update documentation and tests when contributing features.

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## License

Licensed under the [MIT License](LICENSE.md).

---

## Changelog

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
  * Initial release of Franz.Common.Mediator.
  * Core Dispatcher, Commands, Queries, Notifications.
  * Basic Pipelines (Logging, Validation).
  * EF integration with DbContextBase.
  * Added Observability hooks (MediatorContext, IMediatorObserver).
  * Console observer provided for testing/demo.
  * Support for optional telemetry (tracing/correlation).



* **Diagnostics**

  * Structured results for better logging and observability.

* **Consistency**

  * HTTP and Messaging now share the same contracts and patterns.

Version 1.3.2

Introduced Error abstraction with Error class and standard codes (NotFound, Validation, Conflict, Unexpected).

Extended Result<T> to integrate seamlessly with Error.

Added ResultExtensions for ergonomic .ToFailure<T>() and .ToResult() conversions.

Version 1.3.3

Refined Validation pipeline with FluentValidation adapter.

Improved Transaction pipeline with options support (rollback rules).

Bugfixes: ensured streaming dispatcher yields properly with observability.

Version 1.3.4

🔥 Removed AutoMapper coupling from the framework.

Mapping responsibilities now belong to the Application layer.

Framework remains reflection-free, adapter-friendly, and lighter.

Cleaner separation of concerns, more flexible design.

Version 1.3.5

Began migration away from MediatR to native Franz.Mediator.

Rewired MessagingPublisher and MessagingInitializer to use Franz abstractions.

Updated DI extensions to reduce tight coupling to Microsoft.Extensions.DependencyInjection.

Fixed ServiceCollection extension failures, ensuring registrations are isolated in Franz.Common.DependencyInjection.Extensions.

Version 1.3.6

🚀 Completed full removal of MediatR dependency.

IIntegrationEvent now inherits from INotification, enabling seamless mediator + Kafka pipelines.

MessagingPublisher.Publish is now Task-based (async/await support, proper error propagation).

MessagingInitializer scans Franz.Mediator.INotificationHandler<> instead of MediatR handlers.

Core libraries are now DI-free — adapters exist for MS.DI and can be extended to others (Autofac, Lamar, etc.).

Minimal rewiring required outside of DI + Messaging, proving strong architectural boundaries.

v1.3.9 – Database Stability Fixes

Fixed incorrect default port fallback (3308 → now correct defaults per provider: MariaDB 3306, Postgres 5432, SQL Server 1433, Oracle 1521).

Connection string builder now uses 127.0.0.1 instead of localhost to avoid socket/TCP mismatches.

Proper SslMode=None applied by default to avoid unwanted SSL negotiation failures.

Masked passwords in logs for safe diagnostics.

v1.3.10 – Scoped DbContext & Lifecycle

Enforced DbContext resolution through DI scope, preventing “phantom DB” issues.

Corrected EnsureCreated vs Migrate lifecycle usage:

Dev/Test → EnsureDeleted + EnsureCreated

Prod → Migrate() only

Added options to configure drop/create/migrate behavior via DatabaseOptions.

v1.3.11 – Seed & Lifecycle Cleanup

Fixed duplicate seed issues caused by mixing EnsureCreated + Migrate.

Clarified seeding strategy:

Use HasData only once (migrations path).

For dev/test, prefer manual or conditional seeding.

Introduced environment-aware DB lifecycle defaults (no more accidental reseeds).

v1.3.12 – Verbose Logging & Observability

Added LoggingPreProcessor and LoggingPostProcessor with runtime request type detection.

Prefixed logs with [Command], [Query], [Request] for clear business-level observability.

Unified logging across pipelines → no more generic ICommand\1orIQuery`1` names.

Lightweight verbose logs:

Pre → Pipeline → Post lifecycle traced with request names.

Keeps focus on Commands/Queries, not raw SQL noise.