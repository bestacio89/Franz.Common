# Franz.Common.EntityFramework.MariaDB

> Persistence and infrastructure integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.EntityFramework.MariaDB` package provides persistence and infrastructure integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.EntityFramework.MariaDB
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A library within the **Franz Framework** designed to extend **Entity Framework Core** support for **MariaDB** using the **Microting.EntityFrameworkCore.MySql** provider. This package simplifies the configuration and integration of MariaDB with EF Core in .NET applications.

---

## **Features**

- **MariaDB Integration**:
  - Leverages `Microting.EntityFrameworkCore.MySql` for robust MariaDB support.
- **Service Registration**:
  - `ServiceCollectionExtensions` to streamline dependency injection for MariaDB contexts.
- **Compatibility**:
  - Fully integrates with the **Franz.Common.EntityFramework** package for shared utilities and repositories.

---

## **Version Information**

**Current Version:** v2.3.4
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.EntityFrameworkCore.Relational** (10.0.14): Provides relational database functionality for EF Core 10.
- **Microting.EntityFrameworkCore.MySql** (10.0.13): MariaDB/MySQL provider aligned with EF Core 10. This is a community-maintained fork with renamed package/namespace identity, not the original Pomelo package; validate query translation, migrations, and runtime behavior against supported MariaDB versions before release.
- **Franz.Common.EntityFramework**: Shared utilities and patterns for database integration.

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
dotnet add package Franz.Common.EntityFramework.MariaDB  
```

---

## **Usage**

### **1. Configure MariaDB Context**

Define a `DbContext` for MariaDB integration:

```csharp
using Microsoft.EntityFrameworkCore;

public class MariaDbContext : DbContext
{
    public MariaDbContext(DbContextOptions<MariaDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
}
```

### **2. Dependency Injection**

Use `ServiceCollectionExtensions` to register MariaDB contexts:

```csharp
using Franz.Common.EntityFramework.MariaDB.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMariaDbContext<MariaDbContext>(options =>
            options.UseMySql(
                "Server=localhost;Database=MyDatabase;User=root;Password=my_password;",
                new MariaDbServerVersion(new Version(10, 5, 12)) // Specify your MariaDB version
            ));
    }
}
```

### **3. Multi-Database Support**

This package integrates with `Franz.Common.EntityFramework` for managing multiple databases, enabling seamless operations across different database types.

---

## **Integration with Franz Framework**

The **Franz.Common.EntityFramework.MariaDB** package integrates seamlessly with:
- **Franz.Common.EntityFramework**: Provides shared patterns for repositories and database management.
- **Franz.Common**: Core utilities for the framework.

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

### Version 1.6.20
- Updated to **.NET 10.0**

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

**Compatibility status:** `Microting.EntityFrameworkCore.MySql` 10.0.13 declares compatibility with EF Core 10 and MariaDB 11.4 LTS. This resolves the package-version mismatch, but does not prove behavioral equivalence with the former Pomelo provider. Run MariaDB integration tests for model creation, CRUD, relationships, LINQ translation, transactions, and migrations before publishing. See [the repository dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
