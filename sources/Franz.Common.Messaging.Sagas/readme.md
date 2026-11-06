# Franz.Common.Messaging.Sagas

> Saga registration, message-to-saga routing contracts, state persistence abstractions, and orchestration primitives.

## Overview

This package exposes saga registration, state persistence contracts, message correlation, and orchestration primitives. It is separate from transport adapters: an application or transport handler must invoke the saga orchestration entry point for each incoming integration event.

## Scope

The package does not itself consume Kafka, RabbitMQ, or Azure messages. Transport adapters and application handlers own message delivery and invoke saga orchestration.

The current implementation uses runtime type inspection and reflection for saga registration, handler discovery, state assignment, and handler invocation. Do not describe this package as Native AOT-ready or reflection-free.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.Sagas
~~~

## Registration

~~~csharp
using Franz.Common.Messaging.Sagas.Configuration;

var sagaBuilder = services.AddFranzSagas(options =>
{
    options.ValidateMappings = true;
});

sagaBuilder.AddSaga<OrderSaga>();
// Or: sagaBuilder.AddSagaAssembly(typeof(OrderSaga).Assembly);

var provider = services.BuildServiceProvider();
provider.BuildFranzSagas();
~~~

AddFranzSagas registers SagaRouter, SagaExecutionPipeline, SagaOrchestrator, the default audit sink, and the builder. AddSaga<TSaga> registers the saga as transient. AddSagaAssembly and AddSagasFromAssemblyContaining<T> use reflection-based assembly scanning.

BuildFranzSagas finalizes the registered saga types and optionally runs SagaMappingValidator according to ValidateMappings. Call it after the service provider has been built and before saga events are processed.

The application must register an ISagaRepository implementation and IMessagingPublisher. AddFranzSagas does not select or register a persistence provider.

## Saga contracts

- ISaga<TState> exposes State, SagaId, and OnCreatedAsync.
- ISagaState is data-only and exposes ConcurrencyToken.
- ISagaStateWithGuid extends ISagaState with an optional Guid Id.
- IStartWith<TMessage> handles the message that starts a saga.
- IHandle<TMessage> handles a message for an existing saga.
- ICompensateWith<TMessage> handles a compensation message.
- ISagaTransition describes the next transition and optional outgoing integration event.
- ISagaContext provides the execution context to saga handlers.
- ISagaRepository loads, saves, and deletes saga state.

SagaBase<TState> supplies a default state and a public SetId(Guid) method. The start path reads SagaId before calling OnCreatedAsync or the start handler. A saga instance therefore needs a non-empty SagaId before orchestration begins; the current orchestrator does not generate or assign it.

## Persistence boundary

ISagaRepository is the persistence contract. Repository implementations exist in the package's persistence areas, but AddFranzSagas does not select one from FranzSagaOptions.Persistence and does not register one automatically. Register the exact repository implementation and its provider dependencies in the application.

The Persistence, RedisConnectionString, KafkaStateTopic, and EntityFrameworkSchema options are not consumed by AddFranzSagas to configure provider registration. The options are currently configuration data only; do not assume they bind automatically from an appsettings.json section.

## Runtime correlation limitation

**Existing-saga message routing is currently blocked by a contract/implementation mismatch.**

IMessageCorrelation<TMessage>.GetCorrelationId returns string, but SagaOrchestrator.ExtractCorrelationId invokes the interface method with a null target and accepts only a Guid result. The interface method is an instance method and its declared return type is string. As a result, the documented correlation contract cannot successfully resolve an existing saga instance in the current implementation.

Do not rely on continuation or compensation routing as production-ready until the correlation contract and orchestrator implementation are reconciled and covered by executable tests. This is a source-code issue, not a documentation-only limitation.

## Auditing and execution pipeline

The package registers DefaultSagaAuditSink by default and allows additional ISagaAuditSink implementations through AddAuditSink<TSink>(). SagaExecutionPipeline exposes Use(...) for middleware composition. AddFranzSagas does not automatically configure a persistence transaction or distributed transaction around a saga step.

## Operational guidance

- Treat saga delivery as repeatable; handlers and outgoing messages must be idempotent.
- Use explicit correlation and causation identifiers and validate their trust source.
- Register a persistence implementation with concurrency semantics appropriate to the selected store.
- Do not claim atomicity between saga state and an external broker without an explicit outbox/transaction design.
- Add tests for start, continuation, compensation, concurrent updates, persistence failures, and redelivery before production use.

## License

MIT. See the repository [LICENSE](../../LICENSE).
