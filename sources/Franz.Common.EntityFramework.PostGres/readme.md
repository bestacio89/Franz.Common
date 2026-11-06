# Franz.Common.EntityFramework.PostGres

> PostgreSQL EF Core provider dependency for the shared Franz persistence layer.

## Overview

This package supplies Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 for use with Franz.Common.EntityFramework. The NuGet package ID retains the historical PostGres spelling; the provider namespace and EF Core extension method use PostgreSQL/Npgsql naming.

## Installation

~~~bash
dotnet add package Franz.Common.EntityFramework.PostGres
dotnet add package Franz.Common.EntityFramework
~~~

## DbContext configuration

Configure the application's concrete DbContext with EF Core's standard registration API:

~~~csharp
services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("Orders")));
~~~

A Franz persistence context should derive from DbContextBase and receive the dispatcher and any optional auditing or tenant dependencies required by the application. Configure the provider through the application's standard EF Core composition root.

Shared repository, unit-of-work, auditing, soft-delete, concurrency, and tenant query/write behavior is supplied by Franz.Common.EntityFramework.

## Tenant and database isolation

Database routing is controlled by DatabaseOptions.IsolationMode in Franz.Common.EntityFramework:

- SharedDatabase uses a concrete configured database name.
- TenantDatabase requires a validated ITenantExecutionContextAccessor.Current.TenantId and replaces the configured {dbName} token.
- DomainDatabase is an explicit alternative requiring IDomainContextAccessor.GetCurrentDomainId().

TenantDatabase never falls back to domain identity. Missing context fails closed. See the [shared EF Core guide](../Franz.Common.EntityFramework/readme.md).

## PostgreSQL row-level security

For shared-database deployments that require database-enforced tenant isolation, define PostgreSQL row-level security policies in migrations. Set the tenant session variable inside the same explicit transaction using a transaction-local setting such as set_config(..., true). Do not use a session-level setting with pooled connections because it can leak between tenants. The application role should not own protected tables or have BYPASSRLS.

EF sensitive-data logging is disabled by default and should remain disabled in production unless explicitly required and reviewed.

## Provider qualification

Run the PostgreSQL Testcontainers suite for model creation, migrations, CRUD, relationships, LINQ translation, transactions, concurrency, and supported bulk operations.

## License

MIT. See the repository [LICENSE](../../LICENSE).
