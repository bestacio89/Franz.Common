# Franz.Common.Messaging.Sagas

> Saga orchestration, lifecycle management, and persistence adapters for long-running workflows.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Sagas
```

## Overview

The package provides:
- Saga contracts and handler-role registration.
- Explicit saga-instance identity resolution for non-start messages.
- An orchestrator that validates lifecycle state, executes handlers, and persists transitions.
- In-memory, EF Core, MongoDB, and Cosmos DB repository adapters.
- Optional metadata-only audit sink integration.

The package is transport-agnostic. A transport handler must deserialize an integration event and invoke the orchestrator with its correlation, causation, message, and tenant metadata.

## Registration

```csharp
using Franz.Common.Messaging.Sagas.Configuration;

var sagaBuilder = builder.Services.AddFranzSagas(options =>
{
    options.Persistence = "EntityFramework";
    options.ValidateMappings = true;
    options.EnableAuditing = true;
});

sagaBuilder.AddSaga<OrderSaga>();
```

The selected repository's infrastructure dependencies must also be configured by the host:
- `Memory`: package-owned in-memory state store.
- `EntityFramework`: register and configure `SagaDbContext`.
- `Mongo`: register the host's `IMongoDatabase`.
- `Cosmos`: configure the host's Cosmos options and `CosmosSagaDbContext`.

The package does not own database connection strings, database lifecycle, broker configuration, or provider credentials. Repository registration uses `TryAdd`, so an application can register its own `ISagaRepository` implementation before calling `AddFranzSagas`.

Register saga types with `AddSaga<TSaga>()` or the assembly-registration helpers. Finalize registrations after the service provider is built when the host does not use the package's hosted startup validation:

```csharp
app.Services.BuildFranzSagas();
```

The package also registers a hosted startup validator. Mapping validation requires every non-start message contract to have a matching `ISagaIdResolver<TMessage>` implementation on the saga.

## Identity and message roles

- `SagaId` is the durable identity of one saga instance and the repository key.
- `CorrelationId` groups related messages; it is not a saga repository key.
- `CausationId` identifies the message that caused the current execution.
- `MessageId` is transport-level message identity. The orchestrator records the latest processed ID. When an `IInboxStore` is registered, the dispatch handler checks and registers completed message IDs. Registration occurs after orchestration succeeds, so handler failures remain retryable; concurrent deliveries can still race because the existing inbox contract does not expose a transaction shared with saga state and outbox writes.
- `TenantId` scopes repository operations. `null` represents the default non-tenant scope; supplied values must not be empty or whitespace.

A start handler creates the saga instance. For a non-start message, the saga must implement `ISagaIdResolver<TMessage>` and return the non-empty `SagaId` of the target instance. Do not derive this identity from correlation metadata.

Handler roles are mutually exclusive for a given message type:
- `IStartWith<TMessage>`: creates a saga instance.
- `IHandle<TMessage>`: advances a running saga.
- `ICompensateWith<TMessage>`: executes a compensation step after compensation has been requested.

## Lifecycle contract

The persisted lifecycle states are:

| State | Accepted handler role | Meaning |
|---|---|---|
| `Running` | Step handlers | Normal forward execution |
| `Compensating` | Compensation handlers only | Forward execution is suspended while compensation proceeds |
| `RetryScheduled` | None | Reserved for a future durable scheduler; currently unsupported |
| `Completed` | None | Successful terminal state |
| `Failed` | None | Failed terminal state |

A handler returns an `ISagaTransition`:
- `Continue(message?)`: persist the current state as `Running`, or keep it `Compensating` during compensation.
- `Complete(message?)`: persist `Completed` and its completion timestamp.
- `Compensate(message, error?)`: require an outgoing integration event, persist `Compensating`, and dispatch the compensation request.
- `Fail(error)`: require an error, persist `Failed`, then surface `SagaFailedException`.
- `Retry(delay?, error?)`: rejected before persistence because no durable retry scheduler is configured.

Compensation order is application-defined and sequential. A compensation handler may emit the next compensation command/event; the next compensation handler runs only after that message is delivered. While the saga is `Compensating`, ordinary step messages are rejected. The orchestrator does not persist or validate the expected next compensation message, infer a compensation stack, or execute compensations in reverse order automatically. Saga implementations must validate domain-specific compensation ordering against their persisted state.

Every successful transition is persisted with the repository's optimistic concurrency contract. Concurrent updates using the same expected version must not both succeed. A failed transition is surfaced only after its `Failed` state has been saved.

## Persistence and delivery guarantees

Supported repository selections are `Memory`, `EntityFramework`, `Mongo`, and `Cosmos`. The host supplies provider configuration. Repository adapters enforce tenant-scoped identity and optimistic concurrency; provider integration tests exercise the configured database implementations.

When `IMessageStore` is registered, outgoing integration events are converted through the Messaging package's canonical `IntegrationEventMessageBuilderStrategy` and persisted through the existing outbox contract. The existing `OutboxPublisherService` can then deliver them asynchronously and recover pending records after process restarts. Without `IMessageStore`, the orchestrator retains the direct-publish compatibility path.

The saga state write and outbox `SaveAsync` call are not guaranteed to share a transaction by the current contracts. A failure between those operations can therefore still leave state advanced without its outgoing message. Likewise, inbox receipt registration follows successful orchestration and is not atomic with saga/outbox persistence. These integrations provide durable outbox delivery and best-effort post-success duplicate suppression, not atomic inbox/outbox semantics, exactly-once delivery, or protection against concurrent duplicate execution. Provider-specific transaction enlistment using the same database transaction/session/batch is required before stronger guarantees can be claimed.

Retry is deliberately fail-fast until a durable scheduler can atomically coordinate the retry deadline and saga state. No in-process delay or best-effort timer is substituted for durable scheduling.

## Auditing

Set `EnableAuditing = true` and register one or more `ISagaAuditSink` implementations. Audit records contain lifecycle and message-type metadata, not application state or exception messages. Sink failures are logged after the state transition has committed; audit writes are not transactionally coupled to repository persistence.

## Current limitations

- Durable retry scheduling is not implemented.
- Transactional outbox/inbox integration and general message deduplication are not implemented by the saga orchestrator.
- Compensation sequencing is application-defined; there is no automatic compensation stack.
- Audit writes are best-effort and are not part of the saga persistence transaction.
- Native AOT compatibility must be validated independently for this package and each adapter before it is claimed.

## License

MIT. See the repository [LICENSE](../../LICENSE).
