# Franz.Common.Messaging.EntityFramework

> Provider selection and persistence registration for messaging storage, with EF-specific repository and behavior setup for relational contexts.

## Overview

Provider selection and persistence registration for messaging storage, with EF-specific repository and behavior setup for relational contexts.

## Installation

```bash
dotnet add package Franz.Common.Messaging.EntityFramework
```

## Registration API

The current entry point is `AddDatabase<TDbContext>(IHostEnvironment, IConfiguration)` in `Franz.Common.Messaging.EntityFramework.Extensions`, constrained to `DbContextBase`.

```csharp
using Franz.Common.Messaging.EntityFramework.Extensions;

builder.Services.AddDatabase<AppDbContext>(
    builder.Environment,
    builder.Configuration);
```

The method reads `Database:Provider` and currently switches on:

| Value | Registration |
|---|---|
| `mariadb` | MariaDB EF provider |
| `postgres` | PostgreSQL EF provider |
| `sqlserver` | SQL Server EF provider |
| `mongo` | Mongo context registration |
| `cosmos` | Cosmos context registration |

Provider matching is case-insensitive because the value is lower-cased before the switch. Unsupported or missing values throw `InvalidOperationException`.

**Current implementation caveats:**
- The exception text also lists Oracle, and `IsRelational` includes Oracle, but the switch has no Oracle branch. Oracle is therefore not selectable through this method as currently implemented.
- The Mongo and Cosmos branches register the base `MongoDbContext` and `CosmosDbContextBase` types rather than using the generic `TDbContext` argument. Do not assume a custom generic context is registered by those branches.
- The `IHostEnvironment` parameter is not used by the implementation.

For relational providers, the method additionally registers generic entity repositories and EF behaviors. The Mongo and Cosmos branches do not receive those EF-specific additions.

## Transaction filter

The package's `TransactionFilter` implements `IAsyncMessageActionFilter`, not an MVC action filter. It checks whether the injected EF `DbContext` already has a current transaction after the message action executes; it commits when no exception is recorded and rolls back when one is recorded. It does not begin a transaction itself.

## Persistence semantics

Do not treat this package as a single interchangeable outbox implementation across all providers. Relational persistence uses EF registration and its context/repository behavior. Mongo and Cosmos use their provider-specific context implementations. Verify message-store durability, transaction boundaries, retry behavior, and duplicate handling against the actual provider and messaging host.

## Configuration

Keep provider and connection settings in environment-specific configuration or a secret provider. Integration-test the selected provider with the same major dependency versions and topology used in production.

## License

MIT. See [LICENSE](../../LICENSE).
