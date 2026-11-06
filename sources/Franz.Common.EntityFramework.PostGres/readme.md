# Franz.Common.EntityFramework.PostGres

> PostgreSQL EF Core provider registration for Franz `DbContextBase` contexts.

## Overview

The adapter configures Npgsql using shared Franz `DatabaseOptions` and the common database-target resolver.

## Installation

```bash
dotnet add package Franz.Common.EntityFramework.PostGres
```

## Registration

```csharp
using Franz.Common.EntityFramework.Postgres.Extensions;

builder.Services.AddPostgresDatabase<ApplicationDbContext>(builder.Configuration);
```

The extension binds the `Database` section through `AddDatabaseOptions`, resolves the effective database name, configures `UseNpgsql`, and registers the concrete context as `DbContextBase`.

## Configuration

```json
{
  "Database": {
    "ServerName": "localhost",
    "DatabaseName": "application",
    "UserName": "application",
    "Password": "<secret>",
    "Port": 5432,
    "SslMode": "Require",
    "IsolationMode": "SharedDatabase",
    "EnableSensitiveDataLogging": false
  }
}
```

The configured `SslMode` is parsed as Npgsql's `SslMode` enum; the adapter fallback is `Disable`. It also has development-oriented `postgres/password` credentials. Explicitly configure deployed connection fields, use an appropriate TLS mode such as certificate-validated verification for the environment, store credentials in a secret manager, and keep sensitive-data logging disabled in production.

## Multi-tenancy and database routing

The adapter delegates database-name selection to `DatabaseTargetResolver`:

- `SharedDatabase` uses a concrete database name unchanged.
- `TenantDatabase` requires a `{dbName}` token and a validated tenant execution context.
- `DomainDatabase` requires a `{dbName}` token and a validated domain context.

Missing required execution context fails closed. A derived context must forward the tenant accessor and the database/multi-tenancy options to `DbContextBase` for model validation and tenant filters.

## License

MIT. See the repository [LICENSE](../../LICENSE).
