# Franz.Common.EntityFramework.MariaDB

> MariaDB EF Core provider dependency for the shared Franz persistence layer.

## Overview

This package supplies the MariaDB/MySQL EF Core provider dependency used with Franz.Common.EntityFramework. It does not define a Franz-specific AddMariaDbDbContext extension or a separate tenant connection-string resolver.

## Installation

~~~bash
dotnet add package Franz.Common.EntityFramework.MariaDB
dotnet add package Franz.Common.EntityFramework
~~~

The package currently references Microting.EntityFrameworkCore.MySql 10.0.13 and Microsoft.EntityFrameworkCore.Relational 10.0.14. Microting.EntityFrameworkCore.MySql is a community-maintained provider fork; do not describe it as the original Pomelo package.

## DbContext configuration

Configure the application's concrete DbContext with the provider's EF Core extension methods. A Franz persistence context should derive from DbContextBase and use the normal EF Core registration path:

Configure the application's concrete DbContext with EF Core's standard AddDbContext registration and the provider's UseMySql extension. Supply the server-version object required by the selected Microting provider release and select a version supported by the actual MariaDB deployment. The application's context constructor must also satisfy its Franz DI dependencies.

The provider package itself contains no Franz service-collection extensions. Shared repository, unit-of-work, auditing, soft-delete, and tenant behavior is supplied by Franz.Common.EntityFramework.

## Tenant and database isolation

Database routing is controlled by DatabaseOptions.IsolationMode in Franz.Common.EntityFramework:

- SharedDatabase uses one configured database name and rejects the {dbName} token.
- TenantDatabase requires a validated ITenantExecutionContextAccessor.Current.TenantId.
- DomainDatabase is an explicit, separate mode requiring IDomainContextAccessor.GetCurrentDomainId().

TenantDatabase never falls back to domain identity. Missing context fails closed. See the [shared EF Core guide](../Franz.Common.EntityFramework/readme.md).

## Provider qualification

Validate model creation, migrations, CRUD, relationships, supported LINQ translation, transactions, concurrency, and bulk-operation behavior against the MariaDB versions deployed by the application. Do not assume that provider-specific translation behavior is identical to Npgsql, SQL Server, Oracle, or Cosmos.

## License

MIT. See the repository [LICENSE](../../LICENSE).
