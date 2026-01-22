# 📦 **Franz.Common.Messaging.Sagas**

### **Version 1.7.5 — Distributed Orchestration Engine for the Franz Framework**

`Franz.Common.Messaging.Sagas` provides **long-running workflow orchestration**, **distributed coordination**, and **deterministic state machines** fully integrated into the Franz architecture.

Sagas in Franz unify:

* **Microservice coordination**
* **Async transactional consistency**
* **Compensating workflows**
* **Message-driven state transitions**
* **Outbox-based reliability**

They operate transport-agnostically and integrate seamlessly with:

* Franz.Common.Messaging
* Franz.Common.Mediator
* Kafka
* RabbitMQ
* Azure CosmosDB
* MongoDB
* EntityFramework
* In-memory transient orchestration

---

## 🔖 **Current Version: 1.7.6**

---

# 🚀 **What’s New in v1.7.5**

### ✔ **CosmosDB & Mongo-backed Saga Stores**

New persistence providers added:

* `MongoSagaRepository`
* `CosmosSagaRepository`

Both support:

* Deterministic serialization (`JsonSagaStateSerializer`)
* Concurrency tokens
* Timestamped audit markers
* Partition-aware storage (CosmosDB)

---

### ✔ **Deterministic Saga ID & State Rules**

Saga identity now follows one deterministic rule:

```
SagaId = derived from IMessageCorrelation<T> interface
```

This eliminates ambiguity across transports and persistence layers.

---

### ✔ **Execution Pipeline Improvements**

* Fully async-safe execution
* Deterministic handler invocation
* Better error propagation
* Handler return types aligned with `Task<ISagaTransition>`

---

### ✔ **Improved DI Boot Sequence**

All saga infrastructure is now guaranteed to resolve **before** message listeners start:

* SagaRouter registered early
* SagaOrchestrator registered before Messaging listeners
* Automatic discovery and finalization via `BuildFranzSagas()`

---

### ✔ **Null-Safety + .NET 10 Compliance**

The entire Saga engine is now:

* `<Nullable>enable`
* `<TreatWarningsAsErrors>true>`
* Aligned with .NET 10 runtime

---

### ✔ **Stabilized Mapping & Reflection**

* Stronger validation in `SagaRegistration`
* Improved scanning for Start, Step, Compensation handlers
* Unified contract resolution

---

### ✔ **Bug Fixes**

* Fixed handler discovery with `ICompensateWith<>`
* Fixed rare DI timing issues
* Fixed correlation-based saga continuation rules

---

# 🧩 **Core Components**

The Saga engine is composed of:

```
ISaga<TState>
    ↓
SagaRegistration
    ↓
SagaRouter
    ↓
SagaOrchestrator
    ↓
SagaExecutionPipeline
    ↓
ISagaRepository (EF / Mongo / Cosmos / Memory)
    ↓
ISagaAuditSink
```

---

# 🧩 **Defining a Saga**

A complete saga is defined by:

```csharp
public sealed class OrderSaga :
    SagaBase<OrderState>,
    IStartWith<OrderCreated>,
    IHandle<PaymentAccepted>,
    ICompensateWith<PaymentFailed>,
    IMessageCorrelation<OrderCreated>,
    IMessageCorrelation<PaymentFailed>
{
    public override Task OnCreatedAsync(ISagaContext ctx, CancellationToken ct)
    {
        State.Id = GetCorrelationId((OrderCreated)ctx.Message);
        State.CreatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task<ISagaTransition> HandleAsync(OrderCreated msg, ISagaContext ctx, CancellationToken ct)
        => SagaTransition.Continue(null);

    public Task<ISagaTransition> HandleAsync(PaymentAccepted msg, ISagaContext ctx, CancellationToken ct)
        => SagaTransition.Continue(null);

    public Task<ISagaTransition> HandleAsync(PaymentFailed msg, ISagaContext ctx, CancellationToken ct)
        => SagaTransition.Continue(null);

    public string GetCorrelationId(OrderCreated message) => message.OrderId;
    public string GetCorrelationId(PaymentFailed message) => message.OrderId;
}
```

Handler discovery uses:

* `IStartWith<TEvent>`
* `IHandle<TEvent>`
* `ICompensateWith<TEvent>`

---

# 🗂️ **Registering Sagas**

```csharp
var builder = services.AddFranzSagas(opts =>
{
    opts.ValidateMappings = true;
});

builder.AddSaga<OrderSaga>();
builder.AddSaga<PaymentSaga>();

services.AddFranzMediator(…);
services.AddRabbitMQMessaging(…);

var app = host.Build();
app.Services.BuildFranzSagas();
```

---

# ⚙️ **Persistence Providers (1.7.5)**

| Provider             | Package / Class          | Status         |
| -------------------- | ------------------------ | -------------- |
| **InMemory**         | `InMemorySagaRepository` | ✓ Stable       |
| **EntityFramework**  | `EfSagaRepository`       | ✓ Production   |
| **MongoDB**          | `MongoSagaRepository`    | ✓ New in 1.7.5 |
| **Cosmos DB**        | `CosmosSagaRepository`   | ✓ New in 1.7.5 |
| **Redis**            | `RedisSagaRepository`    | ✓ Stable       |
| **Kafka Compaction** | (future provider)        | ✓ Stable       |

---

# 📐 **Saga State Model**

All saga states must implement:

```csharp
public interface ISagaState
{
    string? ConcurrencyToken { get; set; }
    DateTime UpdatedAt { get; set; }
}
```

And optionally:

```csharp
public interface ISagaStateWithId
{
    string Id { get; set; }
}
```

---

# 🎯 **Execution Pipeline**

Wraps each handler:

```csharp
await _pipeline.ExecuteAsync(async () =>
{
    var result = handler.Invoke(...);
    if (result is Task t) await t;
});
```

Allows user-defined middlewares:

* telemetry
* retries
* tracing
* error behavior

---

# 🧾 **Auditing & Logging**

`SagaLogEvents` provides structured logs for all key lifecycle events:

* Saga Start
* Step Execution
* Compensation
* Outgoing message
* Errors

Default auditing sink:

```csharp
ISagaAuditSink = DefaultSagaAuditSink
```

Override with:

```csharp
builder.AddAuditSink<MyElasticSink>();
```

---

# 🧪 **Testing**

For unit tests:

```csharp
services
    .AddFranzSagas(o => o.ValidateMappings = true)
    .AddSaga<TestSaga>();

services.AddSingleton<ISagaRepository, InMemorySagaRepository>();
```

The in-memory store is:

* deterministic
* instant
* ideal for workflow validation

---

# 🧱 **Design Philosophy**

The Saga engine adheres to Franz’s core principles:

| Principle                   | Meaning                                                     |
| --------------------------- | ----------------------------------------------------------- |
| **Deterministic**           | Saga identity, mapping, and execution order are guaranteed. |
| **Modular**                 | Stores, handlers, and audit sinks are fully pluggable.      |
| **Transport-agnostic**      | Kafka, RabbitMQ, HTTP, or custom transports.                |
| **Zero runtime reflection** | Only startup scanning.                                      |
| **Safe by default**         | Built-in validation + null-safety.                          |

---


