# 📦 Franz.Common.Mediator (v2.2.18)

A deterministic execution engine for application commands, queries, notifications, and events within the **Franz Framework**.

This mediator is designed as a **structured execution pipeline runtime**, not a simple in-process dispatcher.

It provides:

* explicit handler execution
* composable pipeline stages
* deterministic cross-cutting behavior
* clear separation between command/query/event execution flows

---

# ✨ Features

## Core Dispatcher

* `IDispatcher` as the single entry point for execution
* Scoped execution model per request
* Explicit handler resolution via DI

## Handler Model

Supports:

* `ICommandHandler<TCommand, TResult>`
* `IQueryHandler<TQuery, TResult>`
* `INotificationHandler<TEvent>`
* `IEventHandler<TEvent>`
* `IStreamQueryHandler<TQuery, TStream>`

Handlers are discovered via assembly scanning and registered automatically.

---

## Pipeline Execution Engine

The mediator supports **composable execution pipelines**:

### Command / Query Pipelines

* `IPipeline<TRequest, TResult>`
* Pre-processing + post-processing stages
* Ordered execution chain

### Event Pipelines

* `IEventPipeline<TEvent>`
* Dedicated event execution flow
* Independent from command/query pipelines

---

## Cross-Cutting Pipelines (Composable Modules)

Franz Mediator provides optional pipeline modules:

### Observability

* Logging pipelines
* Serilog enrichment pipelines
* Console observer (optional)

### Validation

* Request validation pipeline
* Event validation pipeline
* Pre-execution validation hooks

### Resilience

* Retry pipeline
* Circuit breaker pipeline
* Bulkhead isolation pipeline
* Timeout pipeline

### Transactional Control

* Transaction pipeline for execution boundaries

---

# 🧭 Execution Model

The Franz Mediator follows a strict execution flow:

```text id="exec_model"
Dispatcher
  ↓
Pre-Pipeline Stages
  ↓
Handler Execution
  ↓
Post-Pipeline Stages
  ↓
Observers (optional)
```

For events:

```text id="event_model"
Dispatcher
  ↓
Event Pre-Pipeline
  ↓
Event Handlers
  ↓
Event Post-Pipeline
```

---

# 📐 Design Principles

## 1. Deterministic Execution

Pipeline execution order is explicit and predictable.

## 2. Composable Behavior

Cross-cutting concerns are modular pipeline components.

## 3. No Hidden Magic

No implicit behavior chains outside registered pipelines.

## 4. Clear Execution Boundaries

Commands, queries, and events are distinct execution models.

## 5. DI-Native Design

All behavior is composed through `IServiceCollection`.

---

# ⚙️ Basic Usage

## 1️⃣ Register Mediator

```csharp id="med1"
services.AddFranzMediator(
    new[] { typeof(SomeHandler).Assembly });
```

---

## 2️⃣ Optional Default Setup (Recommended)

```csharp id="med2"
services.AddFranzMediatorDefault();
```

This includes:

* dispatcher
* handler scanning
* logging pipeline
* validation pipeline
* audit pipeline
* transaction pipeline (optional module set)

---

## 3️⃣ Sending Commands / Queries

```csharp id="med3"
var result = await dispatcher.Send(new CreateHeroCommand());
```

---

## 4️⃣ Publishing Notifications / Events

```csharp id="med4"
await dispatcher.Publish(new HeroCreatedEvent());
```

---

# 🧩 Pipeline Composition Model

Pipelines are **additive and ordered via registration**:

```csharp id="med5"
builder.Services.AddFranzSerilogAuditPipeline()
                .AddFranzEventValidationPipeline()
                .AddFranzSerilogLoggingPipeline()
                .AddFranzTelemetry(env, config);
```

Each pipeline is:

* independently composable
* scoped
* executed in registration order

---

# ⚠️ Architectural Boundaries

This mediator:

## DOES

* execute in-process application logic
* orchestrate pipelines
* manage handler lifetimes
* enforce execution policies

## DOES NOT

* handle transport (Kafka, RabbitMQ, HTTP)
* manage background workers
* perform hosting or runtime orchestration outside DI

---


**Current Version:** v2.2.19

## ✨ Added

* `AddFranzMediatorDefault()` as canonical setup method
* clearer separation between command/query/event pipelines
* improved event pipeline isolation model

## 🔧 Changed

* pipeline system explicitly separated into command/query/event flows
* improved DI scanning consistency for handlers
* clarified execution model semantics

## 🧠 Architectural Clarification

* Mediator = execution engine
* Pipelines = behavior composition layer
* Transport systems (Kafka, etc.) are external to this model

---

# 📄 License

MIT License

---

# 🧠 Final Note

Franz Mediator is designed around one principle:

> **Execution must be explicit, composable, and deterministic**

It is not a request dispatcher with optional behaviors —
it is a structured execution runtime for application logic.


