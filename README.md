# **Franz.Common**

**Deterministic Architecture for Event-Driven .NET Microservices**

**Franz.Common** is the core of the **Franz Framework** — a lightweight, modular, correctness-first toolkit designed to eliminate boilerplate, unify architectural patterns, and accelerate the creation of **cloud-ready, event-driven microservices**.

With a **Kafka-first design** (extensible to RabbitMQ, Azure Service Bus, Redis Streams, and HTTP transports), Franz provides:

* **DDD & CQRS abstractions**
* **Mediator pipelines with behaviors**
* **Outbox/inbox + retries + DLQ**
* **Resilience policies (Polly)**
* **Structured logging & correlation**
* **Identity, SSO & tenant propagation**
* **Unified HTTP & Messaging contracts**

Franz is *batteries-included*, but *fully modular*.

---

# **📚 Architecture Documentation**

Franz includes complete, enterprise-grade documentation for institutions and multi-team environments:

### 🧱 **Architecture (C4)**

* [System Architecture](docs/architecture/system-architecture.md)
* [Messaging Flow](docs/architecture/messaging-flow.md)
* [Runtime Lifecycle](docs/architecture/runtime-lifecycle.md)
* (Optional diagrams can be added later)

### 🧭 **Governance**

* [Versioning Policy](docs/governance/versioning-policy.md)
* [Lifecycle & Release Policy](docs/governance/lifecycle-policy.md)
* [Contribution Guidelines](docs/governance/contribution-guidelines.md)

### 🔐 **Security & Compliance**

* [Authentication Model (OIDC/SAML/WS-Fed)](docs/security/authentication-model.md)
* [Audit Logging](docs/security/audit-logging.md)
* [Data Protection & GDPR](docs/security/data-protection.md)

### ⚙️ **Performance & Resilience**

* [Load Testing Results](docs/performance/load-testing-results.md)
* [Resilience Behaviors](docs/performance/resilience-behavior.md)

### 🔌 **Integration & Standards**

* [Integration Architecture](docs/integration/architecture-integration.md)
* [API Error Contract](docs/compliance/api-error-contract.md)
* [Logging Standards](docs/compliance/logging-standards.md)
* [Naming Standards](docs/compliance/naming-standards.md)

A full roadmap is available here:
👉 **[ROADMAP.md](ROADMAP.md)**

---

# **📦 Subpackages**

Franz is designed as a set of small, composable libraries:

### 🧩 **Core & Business**

* `Franz.Common.Business`
* `Franz.Common.Errors`
* `Franz.Common.Identity`

### 🧩 **HTTP**

* `Franz.Common.Http.Bootstrap`
* `Franz.Common.Http.Identity`
* `Franz.Common.Http.Messaging`
* `Franz.Common.Http.Refit`

### 🧩 **Mediator & Pipelines**

* `Franz.Common.Mediator`
* Logging
* Validation
* Caching
* Resilience behaviors

### 🧩 **Messaging**

* `Franz.Common.Messaging`
* `Franz.Common.Messaging.Hosting`
* `Franz.Common.Messaging.Kafka`
* `Franz.Common.Messaging.RabbitMQ`

### 🧩 **Persistence**

* `Franz.Common.EntityFramework`
* `Franz.Common.MongoDB`
* `Franz.Common.AzureCosmosDB`

### 🧩 **Multi-Tenancy & Observability**

* `Franz.Common.MultiTenancy`
* `Franz.Common.Logging`

---

# **🧱 Architecture Philosophy**

Franz follows three core principles:

### **1️⃣ Correctness First**

Architecture is not diagrams — it is *deterministic behavior*.
Franz enforces consistency across:

* HTTP → standardized error contracts
* Messaging → retries, DLQ, correlation
* Persistence → transactional consistency
* Logging → structured, correlated, compliant logs

### **2️⃣ Predictability Over Cleverness**

No hidden magic. Everything is explicit. Everything is testable.

### **3️⃣ Enterprise-Ready Modules**

* multi-tenancy
* polyglot persistence
* distributed tracing
* compliance & governance
* identity across boundaries

Franz is used to bootstrap *multi-year, multi-team* modernization programs.

---

# **🚀 Getting Started**

Add the core:

```bash
dotnet add package Franz.Common --version 1.6.1
```

Use only the modules you need — Franz is fully modular.

Examples:

```bash
dotnet add package Franz.Common.Business
dotnet add package Franz.Common.EntityFramework
dotnet add package Franz.Common.Mediator
dotnet add package Franz.Common.Messaging.Kafka
```

Full guide:
📘 **[docs/getting-started.md](docs/getting-started.md)**

---

# **🏗 Reference Implementations**

Franz.Common does NOT embed sample microservices directly.
Instead, the official templates live in separate repos:

### ⭐ **Franz.Template.WebApi**

A production-ready microservice template using:

* Franz.Mediator
* Franz.Http
* Kafka messaging
* Validation + logging
* Docker + CI/CD

👉 [https://github.com/bestacio89/Franz](https://github.com/bestacio89/Franz)

### ⭐ More templates coming soon:

* Kafka Worker Template
* RabbitMQ Template
* Event-Sourcing Template

Refer to `/samples/README.md` for integration details.

---

# **🛠 Core Features**

### ✔ CQRS & Mediator Pipelines

* Logging
* Validation
* Caching
* Resilience (Polly)
* Metrics & tracing

### ✔ Messaging & Distributed Processing

* Kafka, RabbitMQ
* Inbox / Outbox pattern
* Retry & DLQ
* Idempotency
* Hosted consumers

### ✔ HTTP Modeling

* Unified error contract
* Correlation ID propagation
* API versioning (optional)
* Refit + Polly integration

### ✔ Multi-Tenancy Support

* Tenant resolution
* Propagation across HTTP and Messaging

### ✔ Domain Model Foundation

* Entities & aggregates
* Domain events
* Event dispatching

### ✔ Observability

* Serilog structured logs
* Automatic correlation
* OpenTelemetry hooks

---

# **🧪 Build & Test**

```bash
git clone https://github.com/bestacio89/Franz.Common.git
cd Franz.Common
dotnet build
dotnet test
```

For integration tests (Kafka):

```bash
docker-compose up -d
dotnet test --filter Category=Integration
```

---

# **📈 Changelog**

Full changelog: [changelog.md](changelog.md)

Highlights:

### **1.6.18–1.6.19 – Mapping Refinements**

* Full constructor-aware mapping engine
* Immutable DTO support
* Faster instantiation
* 100% backward compatible

### **1.6.17 – Messaging Orchestration**

* Unified extension naming (`AddKafka*`, `AddRabbitMQ*`)
* Consistent DI patterns across transports
* Improved RabbitMQ integration
* Cross-package synchronization

---

# **🤝 Contributing**

See the governance docs:

* [Contribution Guidelines](docs/governance/contribution-guidelines.md)
* [Versioning Policy](docs/governance/versioning-policy.md)
* [Naming Standards](docs/compliance/naming-standards.md)

---

# **📜 License**

MIT License.

---

# **🦉 Franz Philosophy**

> *“I don’t chase novelty — I chase correctness.”*
> *“Architecture is not complexity — it is clarity under load.”*
> *“Your system must behave the same in January as it does in June.”*

Franz is built for professionals who value stability, predictability, and long-term thinking.

---

