# Franz.Common.Http.EntityFramework

> ASP.NET Core integration for relational EF Core contexts and separate MongoDB/Cosmos context registration.

## Overview

ASP.NET Core integration for relational EF Core contexts and separate MongoDB/Cosmos context registration.

## Installation

```bash
dotnet add package Franz.Common.Http.EntityFramework
```

## Relational database registration

Use `AddRelationalDatabase<TDbContext>(IHostEnvironment, IConfiguration)` for a context derived from `DbContextBase`.

```csharp
using Franz.Common.Http.EntityFramework.Extensions;

builder.Services.AddRelationalDatabase<AppDbContext>(
    builder.Environment,
    builder.Configuration);
```

The method reads `Database:Provider` and supports `mariadb`, `postgres`, `sqlserver`, and `oracle`. Missing or unsupported provider values throw. Provider-specific registration is delegated to the corresponding Franz provider package.

After registering the relational provider, the method adds:

- `TransactionFilter` as a scoped service and as an MVC service filter.
- Generic entity repositories for the context.
- The package's EF behaviors.

The transaction filter does not begin a transaction. After the MVC action executes, it checks whether `DbContext.Database.CurrentTransaction` already exists; it commits that transaction when the action completed without an exception and rolls it back when an exception is present. The transaction must therefore be created by the surrounding persistence/application workflow.

## MongoDB registration

Use `AddMongoDatabase<TMongoContext>(IConfiguration)` for a type derived from `MongoDbContext`.

```csharp
builder.Services.AddMongoDatabase<AppMongoContext>(builder.Configuration);
```

The method requires `Database:Provider` to equal `mongo` (case-insensitive). It delegates to `AddMongoDbContext<TMongoContext>`.

## Cosmos DB registration

Use `AddCosmosDatabase<TCosmosContext>(IConfiguration)` for a type derived from `CosmosDbContextBase`.

```csharp
builder.Services.AddCosmosDatabase<AppCosmosContext>(builder.Configuration);
```

The method requires `Database:Provider` to equal `cosmos` (case-insensitive) and delegates to `AddFranzCosmosDbContext<TCosmosContext>`.

## Provider boundaries

These are three distinct registration paths. MongoDB and Cosmos registration do not flow through the relational transaction-filter/repository/behavior registration path. Do not use the old single `AddDatabase<TDbContext>` sample from this guide's history.

Oracle registration remains present in the current switch despite the historical dependency note describing deprecation. Verify provider compatibility and the repository's current dependency validation before selecting it in a production deployment.

## Operational guidance

Keep connection strings in environment-specific configuration or a secret provider. Validate provider/context alignment during startup and exercise the actual database provider in integration tests. Do not assume an MVC action filter provides a transaction when no current transaction exists.

## License

MIT. See [LICENSE](../../LICENSE).
