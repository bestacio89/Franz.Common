# Franz.Common.Messaging.Sagas

> Saga orchestration, lifecycle management, and provider-native atomic persistence.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Sagas
```

## Overview

The package provides:
- Saga contracts and handler-role registration.
- Automatic inbound integration-event notification handlers for registered saga message contracts.
- Explicit saga-instance identity resolution for non-start messages.
- Lifecycle-gated orchestration with optimistic concurrency.
- In-memory, EF Core, MongoDB, and Cosmos DB persistence adapters.
- Metadata-only audit sink integration.

The standard Messaging pipeline deserializes an inbound integration event and publishes it through Franz Mediator. `AddSaga<TSaga>()` registers the Saga notification handler for the exact integration-event contracts declared by that saga. No separate transport-specific Saga strategy executer is required.

The notification handler reads the original `Message` from `IMessageContextAccessor`, obtains tenant identity only from the validated `ITenantExecutionContextAccessor`, and passes message identity, correlation, causation, and tenant context to the orchestrator.

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

The host supplies provider configuration:
- `Memory`: package-owned in-memory saga state. This adapter is intended for tests and development and does not provide durable inbox/outbox guarantees.
- `EntityFramework`: register and configure `SagaDbContext`; apply an EF migration containing the `SagaInbox` and `SagaOutbox` tables.
- `Mongo`: register the host's `IMongoDatabase`; the deployment must support multi-document transactions (replica set or sharded cluster).
- `Cosmos`: configure the host's Cosmos options and `CosmosSagaDbContext`. The existing `SagaStates` container must use `TenantId` as its partition key.

The package does not own connection strings, database lifecycle, broker configuration, or provider credentials. Repository registration uses `TryAdd` so applications can provide their own `ISagaRepository`. The selected saga persistence adapter registers the existing `IInboxStore`, `IMessageStore`, and `IUnitOfWork` contracts with provider implementations that share the same transaction context.

Register saga types with `AddSaga<TSaga>()` or the assembly-registration helpers. Finalize registrations after the service provider is built when the host does not use the package's hosted startup validation:

```csharp
app.Services.BuildFranzSagas();
```

The package also registers a hosted startup validator. Mapping validation requires every non-start message contract to have a matching `ISagaIdResolver<TMessage>` implementation on the saga. Every saga message contract must implement `IIntegrationEvent`; closed notification-handler registrations are created for those contracts when `AddSaga<TSaga>()` is called.

## Identity and message roles

- `SagaId` is the durable identity of one saga instance and the repository key.
- `CorrelationId` groups related messages; it is not a saga repository key.
- `CausationId` identifies the message that caused the current execution.
- `MessageId` is transport-level message identity. The inbound Saga notification handler atomically claims it through the existing `IInboxStore` before executing saga handlers when an original message context is available.
- `TenantId` scopes repository operations. It is taken from the validated tenant execution context, not copied from an untrusted message header. `null` represents the default non-tenant scope.

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
- `Compensate(message, error?)`: require an outgoing integration event, persist `Compensating`, and enqueue the compensation request.
- `Fail(error)`: persist `Failed`, commit the transaction, then surface `SagaFailedException`.
- `Retry(delay?, error?)`: rejected before persistence because no durable retry scheduler is configured.

Compensation order is application-defined and sequential. A compensation handler may emit the next compensation command/event; the next compensation handler runs only after that message is delivered. While the saga is `Compensating`, ordinary step messages are rejected. The orchestrator does not persist or validate the expected next compensation message, infer a compensation stack, or execute compensations in reverse order automatically. Saga implementations must validate domain-specific compensation ordering against their persisted state.

Every successful transition is persisted with the repository's optimistic concurrency contract. Concurrent updates using the same expected version must not both succeed.

## Inbound dispatch and failure propagation

The inbound path is:

1. The transport listener reconstructs the Franz `Message` envelope.
2. `MessagingStrategyExecuter` resolves the canonical `ClassName` header, deserializes the integration event, and publishes it through `IDispatcher`.
3. The registered Saga notification handler receives the concrete event and the original message context.
4. The handler establishes one persistence unit of work and invokes the orchestrator.

Inbound integration-event notifications use `StopOnFirstFailure`. A handler failure therefore propagates back to the transport listener instead of being swallowed by the Mediator's default `ContinueOnError` policy. The configured transport retry/dead-letter policy can then decide the delivery disposition. This fail-fast rule applies to all handlers of an inbound integration event, not only Saga handlers.

## Atomic persistence boundary

For a single incoming message, the Saga notification handler performs one persistence unit of work:

1. Begin the provider's `IUnitOfWork`.
2. Atomically claim the incoming message through `IInboxStore.TryRegisterAsync` when a transport message context and message identity are available.
3. Execute every matching saga registration.
4. Persist every saga state transition.
5. Enqueue every outgoing integration event through `IMessageStore`.
6. Commit once after all matching saga handlers finish.

If processing fails before commit, the inbox receipt, all saga transitions, and all outgoing outbox records roll back together. A duplicate receipt skips execution. The inbox receipt is registered before handler execution, but it is not committed until the entire unit succeeds. Local in-process notification publication without a transport message context has no transport message ID and therefore cannot use message-level inbox deduplication.

Provider implementations:

| Provider | Shared transaction context |
|---|---|
| EF Core / relational | One scoped `SagaDbContext` and one database transaction. The saga repository, EF inbox, EF outbox, and `EfUnitOfWork<SagaDbContext>` share that context. |
| MongoDB | One `IClientSessionHandle` transaction shared by saga state, inbox, and outbox collections in the same database. The server deployment must support transactions. |
| Cosmos DB | One EF Cosmos `SaveChanges` transactional batch. Saga state, inbox, and outbox documents share the existing `SagaStates` container and the same `TenantId` logical partition. Cross-container or cross-partition atomicity is not claimed. |
| Memory | In-memory state only. It is not a durable transactional inbox/outbox provider. |

The transaction abstraction is the persistence `IUnitOfWork`; `IMessagingTransaction` remains a transport/broker transaction and is not used for database coordination.

## Delivery semantics

The RabbitMQ adapter maps Franz `Message.Id` to AMQP `BasicProperties.MessageId` and `Message.CorrelationId` to AMQP `CorrelationId`; it restores those values on consumption and preserves the `x-causation-id` envelope header. The built-in RabbitMQ sender and publisher both apply this mapping. External RabbitMQ producers must supply a non-empty GUID message ID to obtain stable inbox deduplication. For backward compatibility, deliveries with a missing or invalid ID receive a new local ID; those legacy deliveries cannot be deduplicated reliably across redelivery.

The database commit atomically records inbox receipt, saga state, and outgoing messages. The outbox publisher sends those records after commit. Publication is **at-least-once**: a crash after broker publication but before marking an outbox record as sent can cause redelivery. Consumers and external side effects must therefore be idempotent. Exactly-once delivery across a database and broker is not claimed.

Hosts without the provider outbox registration retain the direct-publish compatibility path; that path does not provide the atomic inbox/state/outbox guarantee.

## Auditing

Set `EnableAuditing = true` and register one or more `ISagaAuditSink` implementations. Audit records contain lifecycle and message-type metadata, not application state or exception messages. Sink failures are logged and remain best-effort; audit writes are not part of the persistence transaction.

## Current limitations

- Durable retry scheduling is not implemented.
- The in-memory adapter is non-durable and does not provide atomic inbox/outbox processing.
- Cosmos atomicity is limited to one container and one logical partition per incoming message.
- MongoDB transactions require a transaction-capable deployment.
- Compensation sequencing is application-defined; there is no automatic compensation stack.
- Audit writes are best-effort and are not part of the saga persistence transaction.
- Native AOT compatibility must be validated independently for this package and each adapter before it is claimed.

## License

MIT. See the repository [LICENSE](../../LICENSE).
