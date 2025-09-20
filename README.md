Here’s the **polished README.md for Franz.Common v1.4.2**:

---

# **Franz.Common**

**Franz.Common** is the heart of the **Franz Framework** — a lightweight, modular framework that streamlines the development of **event-driven microservices**.
It was born to reduce boilerplate and architectural complexity in modern .NET systems, with **Kafka-first** design, but extensible to **RabbitMQ, Azure Service Bus, Redis, and HTTP APIs**.

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
dotnet add package Franz.Common --version 1.4.2
```

Or install subpackages (e.g., `Business` + `EntityFramework`):

```bash
dotnet add package Franz.Common.Business --version 1.4.2
dotnet add package Franz.Common.EntityFramework --version 1.4.2
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

# 🆕 Franz Framework 1.4.x – The Observability & Resilience Era

### 🌟 Highlights

* Polly-based pipelines: Retry, CircuitBreaker, Timeout, Bulkhead.
* Unified caching layer (Memory, Distributed, Redis).
* OpenTelemetry tracing with Franz tags.
* Correlation ID propagation (Franz.Common.Logging).
* ASP.NET Core bootstrappers for HTTP + Refit clients.
* `DbContextBase`: auditing + soft deletes + event dispatch.

---

## 📌 Changelog
### **1.4.4** — Stability Meets Firepower

* Massive improvements in logging & hybrid config
* Cleaner DI registration across the board
* Elastic APM opt-in for production scenarios
* Performance boosts in mediator pipelines

### **1.4.2**

* Removed `SaveEntitiesAsync` → everything flows through `SaveChangesAsync`.
* Removed obsolete `DbContextMultiDatabase`.
* Alignment with EntityFramework & Business packages.

### **1.4.1**

* Patch bump, doc updates.

### **1.4.0**

* Migrated to **C# 12 typing conventions**.
* Resilience pipelines integrated.
* Observability (Serilog + OTEL) baked in.

➡️ See [CHANGELOG.md](CHANGELOG.md) for full history (1.2.65+).

---

🔥 With `Franz.Common`, you can bootstrap a Kafka-ready, resilient, multi-tenant .NET microservice with **one line of code**.

