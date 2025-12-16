<p align="center">
  <img width="180" src="Docs/assets/FranzLogo.png" alt="Franz Logo"/>
</p>

<h1 align="center">Franz.Common</h1>
<p align="center"><b>Deterministic Architecture for Event-Driven .NET Microservices</b></p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10%2B-blueviolet" />
  <img src="https://img.shields.io/badge/Architecture-Clean%20%7C%20DDD%20%7C%20CQRS-brightgreen" />
  <img src="https://img.shields.io/badge/Messaging-Kafka%20%7C%20RabbitMQ-orange" />
  <img src="https://img.shields.io/badge/Resilience-Polly-blue" />
  <img src="https://img.shields.io/badge/Observability-OpenTelemetry-yellow" />
  <img src="https://img.shields.io/badge/Multi--Tenancy-Built--In-9cf" />
  <img src="https://img.shields.io/badge/License-MIT-lightgrey" />
  <img src="https://img.shields.io/badge/NuGet-300k%2B%20downloads-success" />
</p>

---

# 📘 Overview

**Franz.Common** is the foundation of the **Franz Framework**, a modular, deterministic architecture layer for **building event-driven microservices in .NET 10**.

It eliminates boilerplate, enforces architectural correctness, and provides **DDD, CQRS, messaging, multi-tenancy, resilience, observability, and identity** capabilities—designed for **scalable, long-lived enterprise systems**.

Franz is **Kafka-first**, but also supports **RabbitMQ, Azure Service Bus, MongoDB, CosmosDB, SQL**, and more.

> **Spring Boot for .NET — but deterministic, clean, and transparent.**

---

# 🎯 Why Franz?

Franz was created to bring **predictability, maintainability, and governance** to distributed .NET systems:

* Reduces **80%+** of architectural boilerplate.
* Enforces **structural correctness** at build time.
* Provides **consistent architecture** across microservices.
* Offers **first-class resilience**, **observability**, and **messaging** patterns.
* Minimizes cognitive load through **unified abstractions**.
* Designed for **enterprise requirements** (multi-tenancy, identity, auditability).

---

# 📦 Subpackages

Franz follows a **"batteries-included but modular"** philosophy.

### **Core**

* `Franz.Common` → Core primitives, serialization, DI, functional utilities.

### **Domain & Application**

* `Franz.Common.Business` → DDD aggregates, domain events, pipelines.
* `Franz.Common.Mediator` → Lightweight CQRS mediator with pipelines.

### **Infrastructure**

* `Franz.Common.EntityFramework` → Auditing, soft deletes, domain event dispatching.
* `Franz.Common.MongoDB` → Mongo outbox/inbox.
* `Franz.Common.AzureCosmosDB` → Cosmos outbox/inbox.

### **Messaging**

* `Franz.Common.Messaging` → Messaging contracts, envelopes, options.
* `Franz.Common.Messaging.Hosting` → Hosted async listeners.
* `Franz.Common.Messaging.Kafka`
* `Franz.Common.Messaging.RabbitMQ`

### **HTTP**

* `Franz.Common.Http.Bootstrap`
* `Franz.Common.Http.Refit`
* `Franz.Common.Http.Identity`
* `Franz.Common.Http.Messaging`

### **Identity**

* `Franz.Common.Identity`
* `Franz.Common.SSO` → Keycloak, OIDC, SAML2, WS-Fed integrations.

---

# 🔐 Security Principles

Franz enforces strict, deterministic security patterns:

* Mandatory **CorrelationId**, **TraceId**, and **TenantId** propagation.
* Deterministic error filters (no sensitive data leakage).
* Centralized **authentication & claims enrichment pipelines**.
* Optional strict mode:

  * no unregistered controllers
  * no unregistered message handlers
  * validation-first execution
* Standardized identity flows across **OIDC, SAML2, Keycloak, WS-Fed**.

These principles make Franz suitable for **regulated environments**, including public institutions and financial sectors.

---

# 🌐 Architecture Overview

```mermaid
flowchart TD

subgraph API Layer
    A[HTTP Request] --> B[Franz.Http Pipeline]
    B --> C[Correlation + Validation + Error Handling]
    C --> D[Controller / Minimal API]
end

subgraph Application Layer
    D --> E[Franz.Mediator]
    E --> F[Command / Query Handlers]
    F --> G[Domain Logic]
end

subgraph Infrastructure Layer
    G --> H[(Database)]
    G --> I[[Kafka Producer]]
    G --> J[[RabbitMQ Producer]]
end

subgraph Messaging Layer
    I --> K[[Kafka Broker]]
    J --> L[[RabbitMQ Broker]]

    K --> M[[Kafka Consumer]]
    L --> N[[Rabbit Consumer]]

    M --> E
    N --> E
end
```

---

# 🔄 Runtime Request Lifecycle

```mermaid
sequenceDiagram
    participant Client
    participant API as Franz.Http
    participant Mediator as Franz.Mediator
    participant Handler
    participant Infra as DB / Messaging

    Client->>API: HTTP Request
    API->>API: Correlation + Validation + Error Filter
    API->>Mediator: Dispatch(Request)
    Mediator->>Handler: Execute Handler
    Handler->>Infra: Query DB / Publish Event
    Infra-->>Handler: Response / Ack
    Handler-->>Mediator: Result
    Mediator-->>API: Standardized Response
    API-->>Client: HTTP 200 / 400 / 500
```

---

# 📨 Messaging Flow (Kafka + Outbox)

```mermaid
flowchart LR

subgraph Application
    A[Command Handler] --> B[Domain Event]
    B --> C[(Outbox Store)]
    C --> D[Outbox Dispatcher]
end

D -->|Publish| E[(Kafka Broker)]
E --> F[Consumer Service]
F --> G[Message Handler]
G --> H[(Database)]
```

---

# 🗺️ Franz Ecosystem Map

```mermaid
flowchart LR

Core[Franz.Common]
Business[Business Layer]
Mediator[Mediator]
HttpBoot[Http Bootstrap]
Refit[Refit Integration]
Identity[Identity + SSO]

subgraph Messaging
  MsgCore[Messaging Core]
  MsgHost[Messaging Hosting]
  Kafka[Kafka Integration]
  Rabbit[RabbitMQ Integration]
end

subgraph Persistence
  EF[EF Core Extensions]
  Mongo[MongoDB Outbox]
  Cosmos[CosmosDB Outbox]
end

Core --> Business
Core --> Mediator
Core --> HttpBoot
Core --> MsgCore
Core --> Identity

Business --> EF
Mediator --> HttpBoot
HttpBoot --> Refit

MsgCore --> MsgHost
MsgHost --> Kafka
MsgHost --> Rabbit

Core --> Mongo
Core --> Cosmos
```

---

# 🏛️ Architecture Enforcement (Franz Tribunal)

Franz includes an optional **architecture test suite** based on ArchUnitNET:

* Enforces **layer boundaries** (Domain → Application → Infrastructure).
* Forbids **circular dependencies**.
* Enforces **immutable DTOs**.
* Validates naming conventions:

  * Commands, Queries, Events
  * Handlers
  * Controllers
* Ensures no domain leakage into infrastructure and vice-versa.
* Ensures messaging boundaries are respected.

This makes Franz suitable for **large organizations**, where maintaining architectural discipline is critical.

---

# 🚀 Getting Started

### Install the core package:

```bash
dotnet add package Franz.Common --version 1.7.0
```

Messaging example:

```bash
dotnet add package Franz.Common.Messaging.Kafka
```

---

# 💡 Key Features

### ✔ DDD/CQRS First-Class

Entities, value objects, aggregates, events.

### ✔ Mediator with Pipelines

Logging, validation, telemetry, resilience, transactions.

### ✔ Messaging First

Outbox/inbox, retries, DLQ, correlation propagation.

### ✔ Observability

Serilog, OpenTelemetry, structured logs.

### ✔ Multi-tenancy

Tenant resolution across HTTP, messaging, pipelines.

### ✔ Polyglot Persistence

SQL, MongoDB, CosmosDB with unified abstractions.

---

# 🛠️ Build & Test

```bash
git clone https://github.com/bestacio89/Franz.Common.git
cd Franz.Common
dotnet build
dotnet test
```

Kafka integration tests:

```bash
docker-compose up -d
dotnet test --filter Category=Integration
```

---

# ⭐ Version 1.7.0 — Azure Messaging Expansion

* Franz.Common v1.7.0 introduces first-class Azure messaging support, completing cloud transport parity while preserving Franz’s deterministic, mediator-driven architecture.

☁️ Azure Messaging Stack

* Azure Service Bus adapter
  Durable brokered messaging with Franz-native mapping and mediator dispatch.

* Azure Event Hubs adapter
  High-throughput, partitioned streaming for Kafka-style workloads.

* Azure Event Grid adapter
  HTTP-based event ingress with subscription validation and mediator integration.

🧭 Azure Hosting Orchestration

* New Azure hosting layer to orchestrate:

* Service Bus consumers

* Event Hubs processors

* Event Grid HTTP endpoints

* Built on Franz.Common.Messaging.Hosting

* Preserves strict separation between transport and hosting

🧠 Architectural Guarantees

* No AutoMapper, no reflection magic

* Deterministic metadata propagation

* Transport-agnostic mediator pipelines

* Azure support added without coupling business logic

* This release completes the Azure messaging pillar of the Franz ecosystem, alongside Kafka and RabbitMQ.

---

# 🛣️ Roadmap
* Graphql Adapters and Implementations
---

# 🏢 Enterprise Adoption & Support

Franz is maintained with enterprise environments in mind.
For support, consulting, integration guidance, or architectural reviews, please contact the maintainer.

---

# 🤝 Contributing

Pull requests welcome — internal contributors preferred.
All PRs must include **tests**, **documentation**, and comply with **Franz Tribunal** rules.

---

# 📜 License

MIT License.

