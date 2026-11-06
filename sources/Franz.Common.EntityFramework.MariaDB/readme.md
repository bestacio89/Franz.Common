# Franz.Common.EntityFramework.MariaDB

> MariaDB EF Core provider registration for Franz `DbContextBase` contexts.

## Overview

This adapter uses the Microting MariaDB EF Core provider and the shared Franz `DatabaseOptions`/database-target resolution contract.

## Installation

```bash
dotnet add package Franz.Common.EntityFramework.MariaDB
```

## Registration

```csharp
using Franz.Common.EntityFramework.MariaDB.Extensions;

builder.Services.AddMariaDatabase<ApplicationDbContext>(builder.Configuration);
```

The extension binds the `Database` configuration section through `AddDatabaseOptions`, resolves the effective database name using the configured `Database:IsolationMode`, configures the MariaDB EF provider, and registers the concrete context as `DbContextBase`.

The provider version is configured in the adapter for MariaDB 11.4. Deploy against a compatible server version. Do not copy examples for Pomelo's `MariaDbServerVersion`; this package uses the Microting provider's `ServerVersion.Create(..., ServerType.MariaDb)` API.

## Configuration

```json
{
  "Database": {
    "ServerName": "localhost",
    "DatabaseName": "application",
    "UserName": "application",
    "Password": "<secret>",
    "Port": 3306,
    "SslMode": "None",
    "IsolationMode": "SharedDatabase",
    "EnableSensitiveDataLogging": false
  }
}
```

The adapter has development-oriented fallbacks including `root/password` and the `library` database name. Explicitly configure every connection field in deployed environments; never rely on those defaults. Use a secret store for credentials and keep sensitive-data logging disabled in production unless a reviewed diagnostic requirement justifies enabling it.

## Multi-tenancy

The adapter uses the shared `DatabaseTargetResolver`. `TenantDatabase` and `DomainDatabase` require the corresponding validated execution context; missing context fails closed. The derived DbContext must forward `ITenantExecutionContextAccessor` and the database/multi-tenancy options to `DbContextBase` when those policies are enabled.

## License

MIT. See the repository [LICENSE](../../LICENSE).
