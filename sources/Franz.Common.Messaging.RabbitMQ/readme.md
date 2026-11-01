# Franz.Common.Messaging.RabbitMQ

> Messaging and distributed integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Messaging.RabbitMQ` package provides messaging and distributed integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Messaging.RabbitMQ
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

Franz now speaks **RabbitMQ**.  
The same architectural contract used for Kafka applies here — **one pipeline, one model, zero spaghetti**.

RabbitMQ is just a transport. Franz does the rest.

---

## 🚀 Getting Started

Install the package:

```bash
dotnet add package Franz.Common.Messaging.RabbitMQ
```

Register RabbitMQ messaging:

```csharp
using Franz.Common.Messaging.RabbitMQ.Extensions;

public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddRabbitMQMessaging(configuration);
}
```

---

## 📦 Features

- **Publisher & Sender** via DI (`IMessagingPublisher`, `IMessagingSender`)
- **Consumers** as hosted listeners
- **Inbox / Outbox** support (MongoDB-backed)
- **Transactional publishing** (`IMessagingTransaction`)
- **Replay strategies** (default + extensible)
- **Message context accessor** for scoped metadata
- **Enforced lifetimes** to prevent invalid wiring
- **Duplicate-safe DI registration**

Kafka and RabbitMQ share the **exact same messaging semantics** in Franz.

---

## ⚖️ Philosophy

> *“We don’t care about MQ.  
> We make it behave.”*

Franz removes broker-specific complexity.  
Choose **Kafka** or **RabbitMQ** — your application code does not change.

---

**Current Version:** v2.3.3

---

## 🆕 v1.7.2 — Stability & Infrastructure Hardening

This release finalizes RabbitMQ as a **first-class transport** in Franz.

### ✔ Highlights

- Fully wired **RabbitMQ infrastructure** (publisher, consumers, hosted services)
- **Inbox & Outbox** validated with real MongoDB containers
- RabbitMQ now behaves **identically to Kafka** at the abstraction level

### 🔧 Improvements

- Correct **RabbitMQ.Client 7.x async channel handling**
- Closed all **DI wiring gaps** in hosted services and tests
- All **message builder strategies** registered by default
- Deterministic startup / shutdown behavior

### 🧪 Reliability

- Tested against **real RabbitMQ + MongoDB** using Testcontainers
- No mocks. No shortcuts. Real brokers, real persistence.

> **Result:** RabbitMQ is production-ready and battle-tested.

---

### v1.6.20

- Migration to **.NET 10**
- Improved messaging DI patterns
- Updated RabbitMQ client
- Alignment of messaging abstractions across transports


### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).


## 2.3.4 tenant execution boundary notes

Franz.Common 2.3.4 improves tenant execution-context propagation and cleanup for supported messaging flows. Verify transport-specific behavior and handler registration requirements in [the execution lifecycle guide](../../docs/multitenancy-execution-lifecycle.md). Tenant-aware EF filters are not a security boundary for raw SQL or APIs that bypass EF query filters.
