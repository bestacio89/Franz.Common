# Franz.Common.Business

> Domain-model primitives for entities, aggregate roots, factories, domain events, audit lifecycle, and identity generation.

## Overview

The Business package provides the framework's domain-layer building blocks. It does not own database persistence or transport delivery; those concerns are implemented by infrastructure packages.

## Installation

```bash
dotnet add package Franz.Common.Business
```

## Entity model

`Entity<TKey>` implements `IEntity<TKey>` and provides:

- Constructor-established identity with a private setter.
- Equality based on entity type and non-transient identity.
- Audit lifecycle fields and `MarkCreated`, `MarkUpdated`, and `MarkDeleted`.
- Soft-delete state through `IsDeleted`.
- A protected constructor accepting the entity identifier.

Use the factory abstraction when the domain entity requires an ID constructor:

```csharp
public sealed class Customer : Entity<Guid>
{
    protected Customer(Guid id) : base(id) { }
}

public sealed class CustomerService(IEntityFactory<Guid, Customer> factory)
{
    public Customer Create() => factory.Create();
}
```

`EntityFactory<TKey, TEntity>` requires a single-argument constructor accepting `TKey`. It compiles and caches the constructor delegate when runtime code generation is available; Native AOT uses preserved constructor metadata without compiling an expression. The generic entity type is annotated so its public and non-public constructors survive trimming. Misconfigured entity types fail when the factory type is initialized.

`AggregateFactory<TAggregate, TEvent>` follows the same construction contract for aggregate roots: a single `Guid` constructor, a compiled delegate on JIT runtimes, and preserved-constructor invocation under Native AOT. Enumeration lookup preserves the public static fields that define each enumeration.

## Aggregate roots and domain events

`AggregateRoot<TEvent>` provides event application, replay/rehydration, version tracking, and access to uncommitted changes. Concrete aggregates register event-application handlers and raise events through the protected `RaiseEvent` method. Persistence and publication of those events remain application/infrastructure responsibilities.

## Optimistic concurrency

Entities can derive from `ConcurrencyTrackedEntity<TKey>`, which inherits `Entity<TKey>` and implements `IConcurrencyTrackedEntity`, or implement the interface directly with a `long ConcurrencyVersion` property. `DbContextBase` initializes the version to 1 on insert and increments it on tracked updates; application code should not mutate it.

## Business registration

`AddBusiness(applicationAssembly)` registers the GUID v7 ID generator, entity factories, Mediator infrastructure, and handlers for the explicitly supplied application assembly. Handler collection delegates to the Mediator source-generated registration registry; the application assembly must be compiled with the Franz Mediator source generator. This is the supported registration path for Native AOT and trimming. The Business AOT smoke suite exercises generated command dispatch, entity and aggregate construction, and enumeration lookup.

The optional `AddBusinessPlatform()` pipelines are separate integrations and are not covered by the Business package's AOT smoke contract; qualify their packages independently before making an application-wide claim.

```csharp
using Franz.Common.Business.Extensions;

builder.Services.AddBusiness(typeof(Program).Assembly);
```

Optional platform pipelines are registered separately:

```csharp
builder.Services.AddBusinessPlatform();
```

The platform helper adds Serilog logging, retry, circuit-breaker, timeout, and bulkhead Mediator pipelines. Register only the composition your application intends to use.

## Architectural boundaries

- Domain entities and aggregates own domain state and invariants.
- Entity factories establish identity during construction.
- Persistence adapters own database mapping, audit application, soft-delete filters, and concurrency enforcement.
- Mediator and messaging packages own dispatch and transport behavior.

## License

MIT. See the repository [LICENSE](../../LICENSE).
