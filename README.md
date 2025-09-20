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

## Why Franz?

Franz doesn’t aim to replace MediatR out of disrespect — in fact, MediatR inspired much of its early design.  
Where MediatR shines as a lean, battle-tested mediator library, Franz extends those concepts with features
that modern enterprise systems demand out-of-the-box:

- ✅ **Pipelines included**: logging, validation, caching, transactions, resilience.  
- ✅ **Environment-aware observability**: verbose in development, lean in production.  
- ✅ **Multi-database adapters**: Postgres, MariaDB, SQL Server, Oracle with safe connection builders.  
- ✅ **Messaging first-class**: Kafka since v1.2.65, designed to plug into RabbitMQ, Azure Service Bus, Redis, gRPC.  
- ✅ **Lean core, optional add-ons**: no hidden dependencies, with integrations (Polly, Serilog, etc.) available as opt-ins.  

Think of Franz as **the next step after MediatR** — still keeping the mediator spirit, but built to be batteries-included
for event-driven, multi-tenant .NET applications.

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

# 🚀 Franz Framework 1.4.0 – The Observability & Resilience Release

Franz Framework isn’t just utilities anymore — it’s a **production-ready microservice starter kit for .NET**, Kafka-first but extensible to RabbitMQ.  
With **1.4.0**, Franz delivers resilience, caching, and observability **out of the box**.

---

## 🌟 Highlights in 1.4.0

- 🔄 **Resilience with Polly**
  - Pipelines for **Retry, CircuitBreaker, Timeout, Bulkhead**.
  - Policies resolved by name from a shared registry.
  - Unified, enriched Serilog logging (correlation ID, request type, policy, elapsed time).
  - Opt-in with a one-liner: `services.AddMediatorPollyRetry("RetryPolicy")`.

- 🗄️ **Caching Everywhere**
  - Providers: **Memory**, **Distributed**, **Redis**.
  - Flexible cache key strategies (default, namespaced).
  - Mediator pipeline with automatic **HIT/MISS detection**.
  - Built-in observability: cache hits/misses logged and exported as OTEL metrics.
  - Settings cache for app flags and long-lived configuration values.

- 🔭 **Distributed Tracing with OpenTelemetry**
  - Automatic **root span per Mediator request**.
  - Enriched with Franz tags: correlation ID, tenant, environment, pipeline.
  - Errors automatically tagged and surfaced in traces.
  - Lightweight by design: Franz produces signals, your app chooses exporters (Jaeger, Zipkin, AppInsights, etc.).

  ✅ **Franz.Common.Http.Refit** support: optional, config-driven Refit client registration via the HTTP bootstrapper.
  - Enable in appsettings: `Franz:HttpClients:EnableRefit = true`.
  - Register typed Refit clients automatically from config (base URL, optional policy, interface type).
  - Works with shared Polly policy registry, correlation/tenant header injection, optional token provider, Serilog + OTEL annotations.


- ⚙️ **Framework Improvements**
  - Opinionated bootstrappers for **Database** and **Messaging** providers (Kafka or RabbitMQ) — config decides, code stays clean.
  - Unified logging model across all pipelines.
  - Reduced boilerplate: resilience, caching, and tracing are now one-liners.

---

Franz 1.4.0 is your **Spring Boot-style starter for .NET microservices** —  
batteries included: Mediator, Kafka/Rabbit, Polly, Caching, OpenTelemetry, Serilog, EF, multi-tenancy.



---
### Franz.Common.Logging – Correlation ID Enhancements (v1.3.14)
- 🔗 **Unified pipeline**: correlation IDs flow consistently across requests, notifications, and mediator pipelines.  
- 📡 **Automatic propagation**: every log entry (requests, DB queries, notifications, responses) carries the same correlation ID.  
- 🌍 **External ID support**: accepts incoming `X-Correlation-ID` headers for distributed tracing across services.  
- 🏛️ **Centralized handling**: correlation ID logic consolidated in `Franz.Common.Logging` for reuse and consistency.  
- 🧵 **Scoped logging**: integrated with `ILogger.BeginScope` and Serilog’s `LogContext` to enrich all logs automatically.  
- ⚙️ **Environment-aware output**: detailed payload logs in development, correlation-focused structured logs in production.  

###   Franz.Common.Http.Refit support: optional, config-driven Refit client registration via the HTTP bootstrapper.
  - Enable in appsettings: `Franz:HttpClients:EnableRefit = true`.
  - Register typed Refit clients automatically from config (base URL, optional policy, interface type).
  - Works with shared Polly policy registry, correlation/tenant header injection, optional token provider, Serilog + OTEL annotations.

➡️ See [CHANGELOG.md](CHANGELOG.md) for the full version history (1.2.65+).
