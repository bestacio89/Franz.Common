# Franz.Common.EntityFramework.Oracle

> Oracle EF Core provider dependency for the shared Franz persistence layer.

## Overview

This package supplies Oracle.EntityFrameworkCore for use with Franz.Common.EntityFramework. It provides the database-provider dependency; concrete DbContext registration remains in the application's composition root.

## Installation

~~~bash
dotnet add package Franz.Common.EntityFramework.Oracle
dotnet add package Franz.Common.EntityFramework
~~~

The package references Oracle.EntityFrameworkCore 10.23.26200 and System.Security.Cryptography.Pkcs 10.0.13.

## DbContext configuration

Configure the application's concrete DbContext with EF Core's standard registration API:

~~~csharp
services.AddDbContext<OrdersDbContext>(options =>
    options.UseOracle(configuration.GetConnectionString("Orders")));
~~~

A Franz persistence context should derive from DbContextBase and receive the dispatcher and any optional auditing or tenant dependencies required by the application. The provider package itself contains no Franz service-collection extensions.

Shared repository, unit-of-work, auditing, soft-delete, concurrency, and tenant query/write behavior is supplied by Franz.Common.EntityFramework.

## Tenant and database isolation

Database routing is controlled by DatabaseOptions.IsolationMode in Franz.Common.EntityFramework. TenantDatabase requires a validated tenant execution context and never falls back to domain identity. DomainDatabase is a separate explicit mode. SharedDatabase rejects a tokenized database name.

See the [shared EF Core guide](../Franz.Common.EntityFramework/readme.md).

## Provider qualification

Validate model creation, migrations, CRUD, relationships, LINQ translation, transactions, concurrency, and bulk-operation behavior against the Oracle server versions supported by the application. Do not infer provider support from another relational adapter.

## License

MIT. See the repository [LICENSE](../../LICENSE).
