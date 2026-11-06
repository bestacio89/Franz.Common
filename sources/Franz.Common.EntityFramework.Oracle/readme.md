# Franz.Common.EntityFramework.Oracle

> Oracle EF Core provider registration for Franz `DbContextBase` contexts.

## Overview

This provider adapter builds an Oracle connection string from the shared Franz `DatabaseOptions` and registers the concrete EF Core context.

## Installation

```bash
dotnet add package Franz.Common.EntityFramework.Oracle
```

## Registration

```csharp
using Franz.Common.EntityFramework.Oracle.Extensions;

builder.Services.AddOracleDatabase<ApplicationDbContext>(builder.Configuration);
```

The extension binds the `Database` section through `AddDatabaseOptions`, resolves the effective database/service name through `DatabaseTargetResolver`, configures `UseOracle`, and registers the context as `DbContextBase`.

## Configuration

```json
{
  "Database": {
    "ServerName": "oracle.example.internal",
    "DatabaseName": "APP",
    "UserName": "application",
    "Password": "<secret>",
    "Port": 1521,
    "IsolationMode": "SharedDatabase",
    "EnableSensitiveDataLogging": false
  }
}
```

The adapter uses Oracle's managed data access connection-string builder. It has development-oriented fallbacks including `system/oracle`; explicitly configure the host, service name, user, and password in deployed environments. Validate connectivity against the actual Oracle deployment, keep credentials in a secret store, and leave sensitive-data logging disabled in production.

## Multi-tenancy

The adapter uses the shared `DatabaseTargetResolver`. `TenantDatabase` and `DomainDatabase` require the corresponding validated execution context and a `{dbName}` token in `Database:DatabaseName`. The derived DbContext must forward the execution accessor and required options to `DbContextBase`.

## License

MIT. See the repository [LICENSE](../../LICENSE).
