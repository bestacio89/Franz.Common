# 📦 Franz.Common.Mediator (v2.3.0)

A deterministic execution engine for application commands, queries, notifications, and events within the **Franz Framework**.

Franz Mediator is not a simple in-process dispatcher.

It is a structured execution runtime providing:

* explicit handler execution
* deterministic pipeline composition
* configurable cross-cutting behavior
* compile-time or runtime composition models
* clear separation between commands, queries, notifications, and events

---

# ✨ Features

## Core Dispatcher

* `IDispatcher` as the single execution entry point
* Scoped request execution model
* Explicit handler resolution through dependency injection
* Optimized execution paths with generated registrations support

---

# Handler Registration Models

Franz Mediator supports two registration models.

## V1 — Reflection Registration

Designed for simplicity and traditional application development.

Features:

* assembly scanning
* runtime handler discovery
* zero generator dependency
* minimal setup

Example:

```csharp
services.AddFranzMediator(
    typeof(CreateHeroHandler).Assembly);
```

Suitable for:

* standard web applications
* internal applications
* rapid development scenarios

---

## V2 — Source Generated Registration

Designed for modern applications requiring deterministic startup and compile-time composition.

Features:

* Roslyn source generation
* zero runtime handler discovery
* Native AOT compatible
* trimming friendly
* compile-time registration validation

Example:

```csharp
services.AddFranzMediatorGenerated();
```

The generator produces:

```csharp
services.AddScoped<
    ICommandHandler<CreateHeroCommand, HeroDto>,
    CreateHeroCommandHandler>();
```

without runtime scanning.

---

# Handler Model

Supports:

* `ICommandHandler<TCommand, TResult>`
* `IQueryHandler<TQuery, TResult>`
* `INotificationHandler<TNotification>`
* `IEventHandler<TEvent>`
* `IStreamQueryHandler<TQuery, TResult>`

Handler rules remain explicit:

* one handler owns one contract
* contracts define execution boundaries
* handlers remain independent units

---

# ⚡ Performance Improvements

## Compile-Time Registration

Previous model:

```
Application startup
        |
        v
Assembly scanning
        |
        v
Reflection inspection
        |
        v
DI registration
```

New generated model:

```
Build time
        |
        v
Roslyn generator
        |
        v
Generated registration code

Application startup
        |
        v
Direct DI registration
```

Benefits:

* reduced startup overhead
* deterministic application composition
* no runtime discovery cost

---

# Pipeline Execution Engine

Franz Mediator continues using composable execution pipelines.

## Command / Query Pipelines

Supports:

* `IPipeline<TRequest,TResponse>`
* pre-processors
* handler execution
* post-processors

---

## Event Pipelines

Dedicated event execution model:

* `IEventPipeline<TEvent>`
* event preprocessing
* event handlers
* event postprocessing

Events remain isolated from request/response execution.

---

# Cross-Cutting Pipeline Modules

## Observability

* logging pipelines
* Serilog enrichment
* observers

## Validation

* command/query validation
* event validation
* audit processors

## Resilience

* retry
* circuit breaker
* bulkhead isolation
* timeout policies

## Transactions

* execution boundary management
* transactional pipeline support

---

# 🧭 Execution Model

## Commands and Queries

```
Dispatcher
    |
    v
Pre Processors
    |
    v
Pipeline Chain
    |
    v
Handler
    |
    v
Post Processors
    |
    v
Observers
```

---

## Events

```
Dispatcher
    |
    v
Event Pre Pipeline
    |
    v
Event Handlers
    |
    v
Event Post Pipeline
```

---

# 📐 Design Principles

## 1. Deterministic Execution

The execution chain is explicit.

No hidden behavior is injected outside registered pipelines.

---

## 2. Dual Composition Strategy

Developers choose the appropriate model:

Reflection when simplicity matters.

Generation when deterministic composition matters.

---

## 3. DI Native

All runtime behavior remains composed through `IServiceCollection`.

---

## 4. Contract Isolation

Commands, queries, notifications, and events remain separate execution models.

---

## 5. Performance Without Complexity Leakage

The framework absorbs optimization complexity.

Application developers do not manually register infrastructure.

---

# ⚙️ Recommended Setup

## Standard Applications

```csharp
services.AddFranzMediatorDefault();
```

Uses:

* reflection registration
* default pipelines
* conventional setup

---

## Modern Applications

```csharp
services.AddFranzMediatorGeneratedDefault();
```

Uses:

* generated handler registration
* deterministic startup
* same pipeline model

---

# Architectural Boundaries

Franz Mediator:

## DOES

* execute application logic
* compose execution pipelines
* manage handler lifetimes
* apply execution policies

## DOES NOT

* replace transport systems
* process messages from brokers
* manage hosted services
* own distributed communication

---

# 🧠 Architectural Evolution

## v2.2

> Mediator as a structured execution runtime.

## v2.3

> Mediator as a structured execution runtime with compile-time composition.

The runtime philosophy remains unchanged:

> **Execution must be explicit, composable, and deterministic.**

The difference is that v2.3 removes unnecessary runtime discovery where modern applications demand maximum determinism.

---
