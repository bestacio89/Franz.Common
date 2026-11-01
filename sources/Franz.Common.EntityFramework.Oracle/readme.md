# Franz.Common.EntityFramework.Oracle

> Persistence and infrastructure integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.EntityFramework.Oracle` package provides persistence and infrastructure integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.EntityFramework.Oracle
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A specialized library within the **Franz Framework** that extends **Entity Framework Core** support for **Oracle Database** using the **Oracle.EntityFrameworkCore** provider. This package simplifies the configuration and integration of Oracle databases in .NET applications, supporting both single-tenant and multi-tenant environments.

---

## **Features**

- **Oracle Database Integration**:
  - Leverages `Oracle.EntityFrameworkCore` for Oracle database support.
- **Service Registration**:
  - `ServiceCollectionExtensions` to streamline dependency injection for Oracle database contexts.
- **Multi-Tenancy Support**:
  - Integrates with `Franz.Common.MultiTenancy` for multi-tenant Oracle database configurations.
- **Compatibility**:
  - Fully integrates with **Franz.Common.EntityFramework** for shared utilities and repository patterns.

---

## **Version Information**

**Current Version:** v2.3.3
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Oracle.EntityFrameworkCore** (10.23.26200): Official EF Core provider for Oracle databases.
- **Franz.Common.EntityFramework**: Core EF utilities and patterns.
- **Franz.Common.MultiTenancy**: Multi-tenancy support for database operations.

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
dotnet add package Franz.Common.EntityFramework.Oracle  
```

---

## **Usage**

### **1. Configuring Oracle Context**

Define a `DbContext` for Oracle integration:

```csharp
using Microsoft.EntityFrameworkCore;

public class OracleDbContext : DbContext
{
    public OracleDbContext(DbContextOptions<OracleDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
}
```

### **2. Dependency Injection**

Use `ServiceCollectionExtensions` to register Oracle database contexts:

```csharp
using Franz.Common.EntityFramework.Oracle.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOracleDbContext<OracleDbContext>(options =>
            options.UseOracle(
                "User Id=myUser;Password=myPassword;Data Source=myDataSource"
            ));
    }
}
```

### **3. Multi-Tenancy Support**

Integrate with `Franz.Common.MultiTenancy` to support multiple tenants:

```csharp
using Franz.Common.MultiTenancy;
using Franz.Common.EntityFramework.Oracle.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMultiTenantOracleDbContext<OracleDbContext>(tenantOptions =>
        {
            tenantOptions.ConnectionStringResolver = tenantId =>
                $"User Id=tenant_{tenantId};Password=tenantPassword;Data Source=myDataSource";
        });
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.EntityFramework.Oracle** package integrates seamlessly with:
- **Franz.Common.EntityFramework**: Shared repository patterns and EF utilities.
- **Franz.Common.MultiTenancy**: Simplifies multi-tenant database configurations.

Ensure these dependencies are installed to fully leverage the library's capabilities.

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

---

## Dependency maintenance — October 2026

Oracle EF Core integration and its test dependency graph are part of the .NET 10 dependency validation pass. The project pins System.Security.Cryptography.Pkcs 10.0.13, and tests explicitly resolve patched SSH.NET 2026.0.0. Run Oracle Testcontainers integration tests before release. See [the repository dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
