# Multi-tenancy execution lifecycle and persistence boundaries

## Execution-context contract

TenantExecutionContext represents immutable, validated tenant identity. Resolution is not authorization. HTTP and messaging adapters validate tenant lifecycle and caller/producer authorization before establishing execution context.

## HTTP requests

- Register the HTTP integration through AddFranzMultiTenancy.
- Place UseFranzMultiTenancy after authentication and before authorization/endpoints.
- The request accessor stores server-established state in HttpContext.Items; inbound tenant headers are resolution inputs, never authoritative ambient state.
- Middleware validates tenant lifecycle, invokes the configured tenant validator and requires an application-provided ITenantRequestAuthorizer for authenticated principals.
- Missing authorizer or denied membership fails with HTTP 403.
- Request context is cleared in a finally path after downstream execution.

## Message and background consumers

Register messaging tenancy services with:

```csharp
services.AddMessagingMultitenancyContext();
```

The application must also register an ITenantStore and ITenantMessageAuthorizer. The producer propagates tenant/domain metadata from validated execution context; message headers remain untrusted input.

ITenantMessageContextInitializer.BeginAsync resolves the tenant from the store, requires active status, applies optional ITenantValidator validation, authorizes the producer, verifies any supplied domain belongs to the tenant and is verified, then initializes the scoped execution accessor.

The initializer supports ambient-message and explicit-message entry points. Custom implementations must implement BeginAsync(Message, CancellationToken) to participate in transport paths that pass the deserialized message directly. The default interface implementation throws NotSupportedException, preserving source compatibility while failing closed.

Framework-managed consumer paths establish a fresh dependency-injection scope per delivery and invoke tenant initialization before handler execution:

- Generic messaging hosted service.
- Kafka hosted service.
- RabbitMQ hosted service.
- Azure Service Bus processor.
- Azure Event Hubs processor.
- Outbox message listener.
- MassTransit Kafka scoped-handler adapter.

The returned IAsyncDisposable owns tenant execution lifetime. The scope is disposed after the complete handler pipeline, including exceptions and cancellation. Ambient message context is cleared in a finally path. Outbox processing creates a new async DI scope per stored message and resolves its message store, inbox store, dispatcher and serializer from that scope.

The generic MassTransit Kafka adapter provides a handler overload receiving the per-delivery IServiceProvider. It maps Kafka headers into a Franz message envelope before tenant validation. Its legacy captured-handler overload fails closed when messaging multitenancy is enabled because it cannot guarantee dependencies are resolved from the delivery scope.

Background jobs must carry explicit tenant identity in authenticated job/message metadata and re-enter through the same validation and authorization boundary. Never capture an HTTP-scoped accessor or reuse a mutable accessor across jobs.

## Persistence guarantees

DbContextBase applies tenant query filters to tenant-owned entities, stamps new entities from the current validated tenant and checks ownership during tracked writes. Tenant identity is immutable after insertion.

The framework repository ExecuteBulkUpdateAsync composes its predicate over the configured DbSet, retaining global query filters, and rejects attempts to assign TenantId. Direct EF Core set-based operations retain query filters but bypass materialization, change tracking, SaveChanges, save interceptors, audit hooks and domain-event dispatch. Callers must explicitly manage audit fields and tracked-instance consistency. Direct low-level set-based calls must not reassign tenant ownership.

Provider translation is provider/version and expression-shape dependent; qualify the exact provider and operations used. Cosmos support must not be inferred from relational ExecuteUpdateAsync behaviour.

## Raw SQL

Global query filters do not rewrite arbitrary SQL. Raw SQL against tenant-owned tables requires a trusted tenant predicate or independent database enforcement such as row-level security. If session state is used, initialize it from validated execution context and reset it before a pooled connection is returned. Parameterize all values.

## Relational qualification

The shared tenant-isolation contract is exercised against PostgreSQL 16, MariaDB 11.4, MySQL 8.4, SQL Server 2022 and Oracle Free 23. Provider-specific migration, transaction, concurrency and SQL translation tests remain required. Cosmos DB and MongoDB require separate contracts.

See [tenant isolation audit](../Docs/tenant-isolation-boundary-audit.md) and [relational provider contract](../Docs/relational-provider-contract.md).
