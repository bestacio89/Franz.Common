Here’s the **full updated README**:

---

# **Franz.Common**

**Franz.Common** is the heart of the **Franz Framework** — a lightweight, modular framework that streamlines the development of **event-driven microservices**.
It was born to reduce boilerplate and architectural complexity in modern .NET systems, with a **Kafka-first** design, but extensible to **RabbitMQ, Azure Service Bus, Redis, and HTTP APIs**.

Franz provides **DDD + CQRS building blocks**, **resilience pipelines**, **auditing**, and **multi-tenancy** support across HTTP and messaging layers — batteries included, but modular.

---

## 📦 Subpackages

Franz is modular: install only what you need.

* **Franz.Common.Business** → DDD + CQRS abstractions, domain events, resilience pipelines.
* **Franz.Common.EntityFramework** → DbContextBase with auditing, soft deletes, domain event dispatching.
* **Franz.Common.Mediator** → Lightweight Mediator with pipelines for caching, logging, validation, resilience.
* **Franz.Common.Http.Bootstrap** → ASP.NET Core bootstrapper (DI, config, pipelines).
* **Franz.Common.Http.Refit** → Refit integration with Polly, logging, tenant/correlation headers.
* **Franz.Common.Logging** → Correlation ID propagation + structured logging with Serilog.
* **Franz.Common.MultiTenancy** → Tenant/domain resolution across HTTP and messaging.
* **Franz.Common.Errors** → Unified error handling models.

---

## 🚀 Why Franz?

Franz doesn’t reinvent the wheel. It builds on proven ideas from **MediatR** and **Polly**, but **extends them into a cohesive framework** for modern microservices.

* ✅ **Pipelines included** → Logging, validation, caching, transactions, resilience.
* ✅ **Environment-aware observability** → verbose in dev, lean in prod.
* ✅ **Multi-database adapters** → Postgres, MariaDB, SQL Server, Oracle.
* ✅ **Messaging first-class** → Kafka out-of-the-box, designed to extend.
* ✅ **Lean core, optional add-ons** → nothing hidden, integrations are opt-in.

Think of Franz as **Spring Boot for .NET microservices** — a batteries-included starter kit.

---

## 🛠 Getting Started

### Installation

Add the core library:

```bash
dotnet add package Franz.Common --version 1.5.4
```

Or install subpackages (e.g., `Business` + `EntityFramework`):

```bash
dotnet add package Franz.Common.Business --version 1.5.4
dotnet add package Franz.Common.EntityFramework --version 1.5.4
```

### Software Dependencies

* **.NET 9+**
* **Kafka 2.6+** (or RabbitMQ/Azure Service Bus with adapters)
* **Confluent.Kafka** client (for Kafka transport)
* **Docker** (for integration testing)

---

## ⚙️ Core Features

### 1. Multi-Tenancy

Works across **HTTP** and **Messaging**.

```csharp
// Startup.cs
services.AddFranzMultiTenancy()
        .AddFranzHttpMultiTenancy()
        .AddFranzMessagingMultiTenancy();

app.UseFranzMultiTenancy();
```

* HTTP resolvers: `HostTenantResolver`, `HeaderTenantResolver`, `JwtClaimTenantResolver`.
* Messaging resolvers: `HeaderTenantResolver`, `MessagePropertyTenantResolver`.

Access anywhere:

```csharp
var tenantId = _tenantContextAccessor.GetCurrentTenantId();
var domainId = _domainContextAccessor.GetCurrentDomainId();
```

---

### 2. Business Layer (DDD + CQRS)

```csharp
builder.Services.AddFranzPlatform(
    typeof(Program).Assembly,
    options => options.DefaultTimeout = TimeSpan.FromSeconds(30));
```

* Entities, Value Objects, Enumerations.
* Aggregates with event sourcing.
* Domain + integration events.
* CQRS support with commands/queries.

---

### 3. Entity Framework Integration

Use `DbContextBase` instead of plain `DbContext`:

* Auditing (`CreatedBy`, `LastModifiedBy`, timestamps).
* Soft deletes (`IsDeleted`, `DeletedOn`, `DeletedBy`).
* Domain event dispatch.
* Global query filters.

```csharp
public class AppDbContext : DbContextBase
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IDispatcher dispatcher, ICurrentUserService user)
        : base(options, dispatcher, user) { }
}
```

---

### 4. Resilience Pipelines (via Mediator)

Automatically registered with `AddFranzPlatform`:

* 🔄 Retry
* ⛔ CircuitBreaker
* ⏱ Timeout
* 🚦 Bulkhead

Configurable in `appsettings.json`:

```json
"Franz": {
  "Resilience": {
    "Retry": { "Enabled": true, "RetryCount": 3 },
    "CircuitBreaker": { "Enabled": true, "FailureThreshold": 5 },
    "Timeout": { "Enabled": true, "TimeoutSeconds": 15 },
    "Bulkhead": { "Enabled": true, "MaxParallelization": 50 }
  }
}
```

---

### 5. Observability

* **Logging** → Serilog integration, correlation IDs flow automatically.
* **Caching** → Memory, Distributed, Redis providers.
* **Tracing** → OpenTelemetry spans enriched with Franz tags (tenant, request type, pipeline).

---

### 6. HTTP & Refit Integration

Enable typed Refit clients with config-only setup:

```json
"Franz": {
  "HttpClients": {
    "EnableRefit": true,
    "Clients": [
      { "Name": "OrdersApi", "BaseUrl": "https://orders.local", "Policy": "RetryPolicy" }
    ]
  }
}
```

Franz injects correlation/tenant headers, applies Polly policies, and enriches with OTEL + Serilog.

---

## 🧪 Build & Test

```bash
git clone https://github.com/bestacio89/Franz.Common.git
cd Franz.Common
dotnet build
dotnet test
```

Integration tests with Kafka:

```bash
docker-compose up -d
dotnet test --filter Category=Integration
```

---

## 🤝 Contributing

Contributions are welcome (internal team preferred).

1. Clone repo.
2. Create a feature branch (`feature/<desc>`).
3. Submit PR.
4. Add tests + docs.

See [CONTRIBUTING.md](CONTRIBUTING.md).

---

## 📜 License

Licensed under the **MIT License**.

---

Perfect — here’s the **main README updated with v1.5.1** so it flows right after 1.5.0:

---

# 🆕 Franz Framework 1.4.x → 1.5.x

### **The Observability & Simplicity Era**

---

## 📌 Changelog
**Latest Version:** `1.5.9`

### ✨ Highlights
- ⚡ By-name fallback mapping (zero config).
- 📑 Profiles with `CreateMap`, `ForMember`, `Ignore`, `ReverseMap`, and `ConstructUsing`.
- 🧩 Expression-based mapping (cached, reflection-minimal).
- 🔧 DI integration with `AddFranzMapping`.
- 🛠 **NEW in 1.5.6** → Assembly scanning for auto-registration of profiles.


## Version 1.5.4 - 1.5.8 - Maintenance Nullability Cleanup
- Dependencies updated
- Documentation Upgrade
- Documentation Cleanup
- Upgraded core package dependencies
- Removed redundant Business.HandlerCollector
- Normalized nullability across bootstrap, messaging, Kafka layers
- Refactored MessagingSender to async-safe implementation
- Structured logging via ILogger (Serilog ready)
- Cleaned ServiceCollectionExtensions with fail-fast guards
- Kafka consumer: fail-fast on invalid payloads, structured exception logging
- Consistent DDD exception usage (NotFoundException, TechnicalException)


### Version 1.5.2 – Reverse Mapping Unlocked 🔄
- Fixed `ReverseMap()` to correctly generate reverse mappings.  
- Replaced expression storage with **string-based property resolution**.  
- Simplified value assignment using reflection (no `.Compile()` errors).  
- Ensured **convention-based mapping fallback** when no explicit map exists.  



### **Older Versions** (summary)
* **1.5.1** – Native Mapping Arrives 
* **1.5.0** – When Aras Becomes Simple 
* **1.4.5** — *Patch Release: Event Semantics*
* **1.4.4** — Logging improvements, hybrid config, Elastic APM opt-in, perf boosts.
* **1.4.2** — Removed `SaveEntitiesAsync`; removed obsolete multi-database DbContext; alignment with EF & Business.
* **1.4.1** — Patch bump & docs.
* **1.4.0** — Migrated to C# 12 conventions; resilience pipelines; observability with Serilog + OTEL.

➡️ Full history available in [CHANGELOG.md](CHANGELOG.md).

---

🔥 With `Franz.Common`, you can bootstrap a Kafka-ready, resilient, multi-tenant .NET microservice with **one line of code**.



