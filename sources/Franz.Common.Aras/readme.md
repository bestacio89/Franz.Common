# Franz.Common.Aras

> ARAS Innovator adapter contracts, REST-backed contexts, mapping factories, and repository base classes.

## Installation

```bash
dotnet add package Franz.Common.Aras
```

## Register the Innovator adapter

```csharp
using Franz.Common.Aras.Extensions.Options;
using Franz.Common.Aras.Innovator.Extensions;

builder.Services.AddArasInnovator(options =>
{
    options.BaseUrl = configuration["Aras:BaseUrl"]
        ?? throw new InvalidOperationException("Missing Aras:BaseUrl.");
    options.AuthToken = configuration["Aras:AuthToken"];
    options.UseDiagnostics = false;
    options.SnapshotFrequency = 50;
});
```

The registration extension binds `ArasInnovatorOptions`, registers the entity and aggregate mapper factories as singletons, registers `IArasEntityContext` and `IArasAggregateContext` as scoped services, and configures typed HttpClients for the Innovator entity and aggregate contexts. Both clients use `BaseUrl` as their base address and add a Bearer authorization header when `AuthToken` is non-empty.

The options type also exposes `UseDiagnostics` (default `false`) and `SnapshotFrequency` (default `50`). The registration method shown does not itself register diagnostic decorators or apply the snapshot frequency; verify the context/decorator composition before relying on those settings.

## Entity context and repository contract

`IArasEntityContext` provides asynchronous query, get-by-ID, save, and delete operations for `Entity<Guid>` models. The generic repository contract exposes:

- `GetByIdAsync(Guid, CancellationToken)`
- `ListAsync(CancellationToken)`
- `AddAsync(TEntity, CancellationToken)`
- `UpdateAsync(TEntity, CancellationToken)`
- `DeleteAsync(Guid, CancellationToken)`

The abstract `ArasEntityRepository<TEntity>` delegates add and update to the context's `SaveEntityAsync`. Its list implementation issues an empty query string; confirm that convention against the target ARAS endpoint and mapping before using it as a general-purpose query.

## Aggregate context and repository contract

`IArasAggregateContext` supports tracking, loading, saving, committing aggregate changes, and retrieving an aggregate snapshot store. The repository contract constrains aggregates to `AggregateRoot<TEvent>` and domain events to `IDomainEvent`. The abstract repository delegates load/save to the context and skips persistence when the aggregate has no uncommitted changes.

## Unit-of-work boundary

`IArasUnitOfWork` defines entity and aggregate contexts plus `CommitAsync` and `RollbackAsync`. The `AddArasInnovator` registration method currently shown does not register an `IArasUnitOfWork` implementation. Do not assume the interface is resolvable from DI without an application-specific implementation or additional registration.

## Configuration and security

Store the base URL and authentication token in a secret provider or environment-specific configuration. The adapter sends the token as a Bearer credential; do not log it or expose it in diagnostics. Validate connectivity, authorization, mapping, and failure behavior against a non-production Innovator environment.

## License

MIT. See [LICENSE](../../LICENSE).
