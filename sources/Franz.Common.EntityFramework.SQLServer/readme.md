# Franz.Common.EntityFramework.SQLServer

> SQL Server EF Core provider registration for Franz `DbContextBase` contexts.

## Overview

This adapter uses Microsoft.Data.SqlClient and the SQL Server EF Core provider. It consumes shared Franz database options and database-target resolution.

## Installation

```bash
dotnet add package Franz.Common.EntityFramework.SQLServer
```

## Registration

```csharp
using Franz.Common.EntityFramework.SQLServer.Extensions;

builder.Services.AddSqlServerDatabase<ApplicationDbContext>(builder.Configuration);
```

The extension binds the `Database` section through `AddDatabaseOptions`, resolves the effective database name with `DatabaseTargetResolver`, configures `UseSqlServer`, and registers the concrete context as `DbContextBase`.

## Configuration

```json
{
  "Database": {
    "ServerName": "sql.example.internal",
    "DatabaseName": "application",
    "UserName": "application",
    "Password": "<secret>",
    "Port": 1433,
    "SslMode": "Preferred",
    "IsolationMode": "SharedDatabase",
    "EnableSensitiveDataLogging": false
  }
}
```

The adapter has development-oriented `sa/password` credentials; explicitly configure deployed connection fields and use a secret manager. It maps `SslMode` through `SslEnforcement` to SqlClient's `Encrypt` option, but the current connection-string builder unconditionally sets `TrustServerCertificate = true`. This bypasses certificate-chain validation. Do not treat `Encrypt` alone as server identity verification; correct this provider behavior before relying on certificate validation in production.

## Multi-tenancy

`TenantDatabase` and `DomainDatabase` use the shared resolver and require a configured `{dbName}` token plus the corresponding validated execution context. The derived DbContext must forward the execution accessor and database/multi-tenancy options to `DbContextBase`.

## License

MIT. See the repository [LICENSE](../../LICENSE).
