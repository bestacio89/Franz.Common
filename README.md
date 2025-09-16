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

1.3.2

Introduced Error abstraction with Error class and standard codes (NotFound, Validation, Conflict, Unexpected).

Extended Result<T> to integrate seamlessly with Error.

Added ResultExtensions for ergonomic .ToFailure<T>() and .ToResult() conversions.

1.3.3

Refined Validation pipeline with FluentValidation adapter.

Improved Transaction pipeline with options support (rollback rules).

Bugfixes: ensured streaming dispatcher yields with observability.

1.3.4

🔥 Removed AutoMapper coupling from the framework.

Responsibility for object mapping now belongs to the Application layer.

Framework remains reflection-free and adapter-friendly.

Cleaner separation of concerns, lighter dependencies, more flexible.