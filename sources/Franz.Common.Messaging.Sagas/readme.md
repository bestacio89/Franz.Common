
# 📦 **Franz.Common.Messaging.Sagas**

### **Version 1.6.21 — Saga Orchestration Engine for the Franz Framework**

Franz.Common.Messaging.Sagas brings **long-running workflows**, **distributed coordination**, and **reliable message-driven orchestration** into the Franz ecosystem.

Sagas allow you to coordinate multiple microservices, enforce consistency across asynchronous processes, and implement *"orchestrated"* event-driven business flows — all while remaining transport-agnostic and fully compatible with:

* **Franz.Common.Messaging**
* **Franz.Common.Mediator**
* **Kafka**
* **RabbitMQ**
* **EntityFramework**
* **Redis**
* **In-Memory workflows**

---

**Current Version**: 1.7.0

---

# 🚀 **What’s New in v1.6.21**

### ✔ Full Saga Infrastructure

* `ISaga<TState>` base interface
* Start / Step / Compensation handler interfaces
* Strongly typed state model (`ISagaState`)
* `SagaTransition` system for outgoing messages

### ✔ Saga Execution Engine

* `SagaOrchestrator`
* `SagaRouter`
* `SagaExecutionPipeline` (middleware-like wrapping)

### ✔ Validations

* `SagaTypeValidator`
* `SagaMappingValidator`
* Full mapping validation at startup
* Prevention of misconfigured handlers

### ✔ Persistence Providers (Pluggable)

* **EntityFramework** (production ready)
* **Redis** (stub)
* **Kafka compacted topics** (stub / future)
* **InMemory** (fast + ideal for unit tests)

### ✔ Logging & Auditing

* Structured logging (`SagaLogEvents`)
* Pluggable audit sinks (`ISagaAuditSink`)
* Included default implementation:
  `DefaultSagaAuditSink → ILogger`

### ✔ `appsettings.json` First-Class Support

* Automatic wiring of persistence providers
* Automatic saga registration
* Optional auditing + validation
* Environment-friendly configuration

---

# 📐 **Architecture Overview**

A fully configured Saga registry consists of:

```
ISaga<TState>
   ↓ discovers
SagaRegistration
   ↓ aggregated into
SagaRouter
   ↓ invoked by
SagaOrchestrator
   ↓ coordinated via
SagaExecutionPipeline
   ↓ persisted in
ISagaRepository (EF / Redis / Mem / Kafka)
   ↓ traced with
ISagaAuditSink
```

---

# 🧩 **Defining a Saga**

A Saga is simply a class implementing:

```csharp
public class OrderSaga : ISaga<OrderState>,
                         IStartWith<OrderCreated>,
                         IHandle<PaymentAccepted>,
                         ICompensateWith<PaymentFailed>
{
    public OrderState State { get; private set; } = new();
    public string SagaId => State.OrderId;

    public Task OnCreatedAsync(ISagaContext context, CancellationToken token)
    {
        State.CreatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public async Task<ISagaTransition> HandleAsync(OrderCreated message, ISagaContext ctx, CancellationToken token) { … }
    public async Task<ISagaTransition> HandleAsync(PaymentAccepted message, ISagaContext ctx, CancellationToken token) { … }
    public async Task<ISagaTransition> HandleAsync(PaymentFailed message, ISagaContext ctx, CancellationToken token) { … }
}
```

Franz automatically discovers handlers through:

* `IStartWith<TMessage>`
* `IHandle<TMessage>`
* `ICompensateWith<TMessage>`

---

# 🗂️ **Registering Sagas**

Using the fluent builder pattern:

```csharp
services
    .AddFranzSagas(Configuration)
    .AddSaga<OrderSaga>()
    .AddSaga<PaymentSaga>();
```

Then finalize:

```csharp
services.BuildFranzSagas(app.Services);
```

---

# ⚙️ **Configuration (appsettings.json)**

```json
{
  "Franz": {
    "Sagas": {
      "Persistence": "EntityFramework",
      "EnableValidation": true,
      "EnableAuditing": true,
      "EntityFrameworkSchema": "sagas"
    }
  }
}
```

Supported persistence values:

| Value               | Provider                             |
| ------------------- | ------------------------------------ |
| `"Memory"`          | Fast, volatile store (default)       |
| `"EntityFramework"` | SQL-backed persistent store          |
| `"Redis"`           | Redis-based store (stub)             |
| `"Kafka"`           | Kafka compacted-topic storage (stub) |

---

# 🧬 **Saga State Persistence**

Every saga has a unique `SagaId`, which is:

* Extracted using `IMessageCorrelation<TMessage>`
* Used as the primary key of the persisted state

Persistence layer implements:

```csharp
ISagaRepository
{
    Task<object?> LoadStateAsync(...);
    Task SaveStateAsync(...);
    Task DeleteStateAsync(...);  // optional
}
```

---

# 🛠️ **Execution Pipeline**

`SagaExecutionPipeline` provides a middleware-like wrapping:

```csharp
await _pipeline.ExecuteAsync(() =>
    handler.Invoke(saga, new object[] { message, ctx, token })
);
```

Allows adding:

* retries
* monitoring
* execution time metrics
* tracing
* global behaviors

---

# 📊 **Auditing & Logging**

### Structured logs:

```csharp
SagaLogEvents.StepStart(logger, sagaType, sagaId, messageType);
SagaLogEvents.StepComplete(logger, sagaType, sagaId, outgoingMessage, error);
SagaLogEvents.HandlerError(logger, sagaType, sagaId, messageType, ex);
```

### Audit record:

`SagaAuditRecord` contains:

* SagaId
* SagaType
* StateType
* StepType
* IncomingMessageType
* OutgoingMessageType
* Duration
* Timestamp
* Serialized state

### Default Sink → ILogger

```csharp
ISagaAuditSink = DefaultSagaAuditSink
```

Users can override:

```csharp
.AddAuditSink<ElasticSagaSink>();
```

---

# 🧩 **DI Extensions**

The extension method:

```csharp
AddFranzSagas(IConfiguration)
```

does the following:

* Loads `FranzSagaOptions`
* Registers `SagaRouter(provider)`
* Registers `SagaOrchestrator`
* Registers execution pipeline
* Configures persistence provider
* Configures audit sinks
* Returns builder (`FranzSagaBuilder`)

Finally:

```csharp
BuildFranzSagas(IServiceProvider)
```

validates + registers all sagas at startup.

---

# 🧪 **Unit Testing**

The in-memory provider is ideal for tests:

```csharp
services
    .AddFranzSagas(Configuration)
    .AddSaga<TestSaga>();

services.Configure<FranzSagaOptions>(opts =>
{
    opts.Persistence = "Memory";
    opts.EnableValidation = true;
});
```

---

# 🧱 **Design Philosophy**

Franz.Common.Messaging.Sagas follows the core Franz principles:

### ✔ Deterministic

Explicit, predictable execution.
Zero ambiguity.

### ✔ pluggable

Storage, logging, and middleware are fully modular.

### ✔ transport-agnostic

Kafka, RabbitMQ, Azure Service Bus, HTTP, or anything else.

### ✔ lightweight

Zero reflection at runtime except for initial discovery.

### ✔ safe by default

Automatic validation + safe fallback persistence.

---

# 🏁 **Conclusion**

Franz.Common.Messaging.Sagas (v1.6.21) brings first-class saga orchestration into the Franz ecosystem, enabling:

* long-running business workflows
* reliable distributed coordination
* strongly-typed message-driven state machines
* seamless integration with the Franz mediator and messaging frameworks
* full auditability
* easy testing
* lightweight, modular design



