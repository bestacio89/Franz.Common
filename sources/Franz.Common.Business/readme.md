# Franz.Common.Business

> Domain model, entity/aggregate factories, identity generation, and repository contracts for Franz applications.

## Overview

This package contains the DDD-oriented business abstractions used by Franz applications: entities, aggregate roots, value objects, domain events, identity generators, factories, and persistence-facing repository contracts.

It does not provide an EF Core repository implementation; that belongs to Franz.Common.EntityFramework.

## Installation

~~~bash
dotnet add package Franz.Common.Business
~~~

## Business registration

~~~csharp
using Franz.Common.Business.Extensions;

services.AddBusiness(typeof(ApplicationAssemblyMarker).Assembly);
services.AddBusinessPlatform();
~~~

AddBusiness requires an explicit application assembly. It registers the Guid V7 ID generator, the open generic entity factory, Franz Mediator infrastructure, and generated handler registration for the selected assembly.

AddBusinessPlatform is optional. It adds the Serilog logging, retry, circuit-breaker, timeout, and bulkhead Mediator pipelines. It does not register the validation or transaction pipelines.

TryAddBusiness accepts a nullable assembly and leaves the service collection unchanged when it is null.

## Entity identity

Entity<TKey> exposes a strongly typed Id with a private setter. Domain code cannot assign identity after construction. Infrastructure has an internal identity setter for persistence/factory scenarios.

The default AddBusiness registration provides IIdGenerator<Guid> backed by Guid.CreateVersion7(). Other ID generator implementations exist, but they are not selected automatically by AddBusiness.

## Entity factory

IEntityFactory<TKey,TEntity> creates entities using an IIdGenerator<TKey>. EntityFactory<TKey,TEntity> requires a public or protected constructor that accepts exactly one TKey argument:

~~~csharp
public sealed class Order : Entity<Guid>
{
    protected Order(Guid id) : base(id) { }
}
~~~

The factory discovers the constructor once per closed generic type, compiles a delegate, and reuses that delegate for subsequent Create calls. Call EntityFactory<TKey,TEntity>.Validate() during explicit application registration when eager constructor validation is required.

The factory uses reflection and Expression.Compile during type initialization. Do not treat it as a warning-clean Native AOT path.

## Aggregate roots

AggregateRoot<TEvent> uses Guid identity and tracks uncommitted domain events. Derived aggregates register event application handlers with Register<TEvent>(Action<TEvent>) and raise new events through RaiseEvent.

The base type exposes:

- GetUncommittedChanges() to inspect pending events;
- MarkChangesAsCommitted() to clear the pending collection;
- Rehydrate(Guid, IEnumerable<TEvent>) to set identity and replay persisted events;
- ReplayEvents(IEnumerable<TEvent>) to apply events without creating new uncommitted changes;
- Version for aggregate event-version tracking.

AggregateFactory<TAggregate,TEvent> creates Guid-based aggregate roots using a protected/public Guid constructor. AddBusiness does not register this aggregate factory automatically; register it explicitly when the application needs it.

## Domain events

IDomainEvent extends IEvent and exposes EventId, nullable Guid CorrelationId, nullable Guid AggregateId, AggregateType, and EventType. It does not define an OccurredOn property in this package's current contract.

## Repository contracts

The business repository contract is IEntityRepository<TEntity,TId> in Franz.Common.Business.Repositories. It is identity-aware and exposes:

- GetByIdAsync and GetAllAsync;
- AddAsync and AddRangeAsync;
- UpdateAsync and UpdateRangeAsync;
- DeleteAsync and DeleteRangeAsync, with an explicit hardDelete argument.

The EF Core implementation and its provider-specific capabilities are supplied by Franz.Common.EntityFramework. Bulk operations are separate EF-specific contracts, not additions to this business interface.

## Architectural boundaries

- Domain entities and aggregates do not depend on EF Core or a database provider.
- Factories own identity initialization.
- Repository contracts describe persistence operations; implementations belong to infrastructure packages.
- Mediator dispatch and handler registration are composed by AddBusiness for the explicitly selected application assembly.
- Runtime factory construction uses reflection/expression compilation; the generated Mediator handler registration does not make the factory path Native AOT-safe.

## License

MIT. See the repository [LICENSE](../../LICENSE).
