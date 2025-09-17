# **Franz.Common.EntityFramework.Postgres**

A dedicated library within the **Franz Framework** that extends **Entity Framework Core** support for **PostgreSQL** using the **Npgsql.EntityFrameworkCore.PostgreSQL** provider. This package simplifies PostgreSQL configuration and integration for .NET applications, supporting both single-tenant and multi-tenant environments.

---

## **Features**

- **PostgreSQL Integration**:
  - Leverages `Npgsql.EntityFrameworkCore.PostgreSQL` for robust PostgreSQL database support.
- **Service Registration**:
  - `ServiceCollectionExtensions` to streamline dependency injection for PostgreSQL database contexts.
- **Multi-Tenancy Support**:
  - Full integration with `Franz.Common.MultiTenancy` for tenant-based database configurations.
- **Compatibility**:
  - Seamless integration with **Franz.Common.EntityFramework** for shared utilities, repository patterns, and extensions.

---

## **Version Information**

- - **Current Version**:  1.3.12
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Npgsql.EntityFrameworkCore.PostgreSQL** (9.0.0): Official EF Core provider for PostgreSQL.
- **Franz.Common.EntityFramework**: Core EF utilities and repository patterns.
- **Franz.Common.MultiTenancy**: Multi-tenancy support for PostgreSQL databases.

---

## **Installation**

### **From Private Azure Feed**
Since this package is hosted privately, configure your NuGet client:

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

Install the package:

```bash
dotnet add package Franz.Common.EntityFramework.Postgres  
```

---

## **Usage**

### **1. Configuring PostgreSQL Context**

Define a `DbContext` for PostgreSQL integration:

```csharp
using Microsoft.EntityFrameworkCore;

public class PostgresDbContext : DbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
}
```

### **2. Dependency Injection**

Use `ServiceCollectionExtensions` to register PostgreSQL database contexts:

```csharp
using Franz.Common.EntityFramework.Postgres.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddPostgresDbContext<PostgresDbContext>(options =>
            options.UseNpgsql(
                "Host=localhost;Database=MyDatabase;Username=my_user;Password=my_password"
            ));
    }
}
```

### **3. Multi-Tenancy Support**

Integrate with `Franz.Common.MultiTenancy` to support multi-tenant PostgreSQL configurations:

```csharp
using Franz.Common.MultiTenancy;
using Franz.Common.EntityFramework.Postgres.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMultiTenantPostgresDbContext<PostgresDbContext>(tenantOptions =>
        {
            tenantOptions.ConnectionStringResolver = tenantId =>
                $"Host=localhost;Database=Tenant_{tenantId};Username=my_user;Password=my_password";
        });
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.EntityFramework.Postgres** package integrates seamlessly with:
- **Franz.Common.EntityFramework**: Shared repository patterns and utilities.
- **Franz.Common.MultiTenancy**: Simplifies tenant-specific database configurations.

Ensure these dependencies are installed to leverage the library's full capabilities.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team. If you have access, follow these steps:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.2.65
- Upgrade version to .net 9

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

