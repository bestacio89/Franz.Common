# Franz.Common.Messaging.Sagas

> Saga registration, state contracts, routing, orchestration, persistence adapters, and audit hooks for long-running workflows.

## Overview

The package contains saga contracts, a registration builder, a message router, an orchestrator, execution pipeline, audit sink, and repository adapters. It is transport-agnostic; a transport handler must invoke saga orchestration when an integration event is received.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Sagas
```

## Registration

```csharp
using Franz.Common.Messaging.Sagas.Configuration;

var sagaBuilder = builder.Services.AddFranzSagas(options =>
{
    options.ValidateMappings = true;
});

sagaBuilder.AddSaga<OrderSaga>();
```

After the service provider is built, finalize registrations:

```csharp
app.Services.BuildFranzSagas();
```

The builder also exposes `AddSaga(Type)`, `AddSagaAssembly(assembly)`, `AddSagasFromAssemblyContaining<T>()`, and `AddAuditSink<TSink>()`. Saga types are registered as transient services.

## Current contracts

- `ISaga<TState>` requires a `State` property, a non-empty `Guid SagaId`, and `OnCreatedAsync`.
- `ISagaState` currently contains a nullable string `ConcurrencyToken`; it does not define an ID or timestamp.
- `IStartWith<TMessage>`, `IHandle<TMessage>`, and `ICompensateWith<TMessage>` declare the message roles handled by a saga.
- `IMessageCorrelation<TMessage>` currently declares an instance method returning `string`.

## Important implementation limitation

The current correlation contract and orchestrator are inconsistent: the interface returns `string`, but the orchestrator invokes the interface method with a null target and then requires the result to be a `Guid`. In addition, the orchestrator requires `SagaId` to be non-empty before it starts the saga; it does not generate the ID automatically.

Consequently, the current correlation-based start/continuation path is not reliably executable. Do not treat saga orchestration or its persistence adapters as production-ready until the correlation contract, invocation target, ID lifecycle, and end-to-end tests are corrected. This guide intentionally does not provide a misleading working-saga sample.

## Persistence and transport boundaries

Repository classes exist for in-memory, EF Core, Cosmos DB, MongoDB, Redis, Kafka, and RabbitMQ. `AddFranzSagas` registers the router, execution pipeline, orchestrator, audit sink, options, and builder; it does not register an `ISagaRepository` implementation. The `FranzSagaOptions.Persistence` value is not currently used to select or register an adapter. Register the intended repository explicitly and do not infer adapter activation from that option. The options model documents Memory, EntityFramework, Redis, and Kafka as its allowed values; the other repository classes are not selected by this options path. Adapter presence does not imply equivalent transactional guarantees. Validate concurrency, atomicity, and failure recovery for the selected repository and transport.

## License

MIT. See the repository [LICENSE](../../LICENSE).
