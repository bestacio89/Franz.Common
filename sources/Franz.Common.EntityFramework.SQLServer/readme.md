# Franz.Common.EntityFramework.SQLServer

> SQL Server EF Core provider dependency for the shared Franz persistence layer.

## Overview

This package supplies Microsoft.EntityFrameworkCore.SqlServer 10.0.14 for use with Franz.Common.EntityFramework. It provides the database-provider dependency; concrete DbContext registration remains in the application's composition root.

## Installation

~~~bash
dotnet add package Franz.Common.EntityFramework.SQLServer
dotnet add package Franz.Common.EntityFramework
~~~

The package also references System.Security.Cryptography.Pkcs 10.0.13.

## DbContext configuration

Configure the application's concrete DbContext with EF Core's standard registration API:

~~~csharp
services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("Orders")));
~~~

A Franz persistence context should derive from DbContextBase and receive the dispatcher and any optional auditing or tenant dependencies required by the application. The provider package itself contains no Franz service-collection extensions.

Shared repository, unit-of-work, auditing, soft-delete, concurrency, and tenant query/write behavior is supplied by Franz.Common.EntityFramework.

## Tenant and database isolation

Database routing is controlled by DatabaseOptions.IsolationMode in Franz.Common.EntityFramework. TenantDatabase requires a validated tenant execution context and never falls back to domain identity. DomainDatabase is a separate explicit mode. SharedDatabase rejects a tokenized database name.

See the [shared EF Core guide](../Franz.Common.EntityFramework/readme.md).

## Provider qualification

Validate model creation, migrations, CRUD, relationships, LINQ translation, transactions, concurrency, and bulk-operation behavior against the SQL Server versions supported by the application. Provider-specific behavior must be tested independently.

## License

MIT. See the repository [LICENSE](../../LICENSE).
