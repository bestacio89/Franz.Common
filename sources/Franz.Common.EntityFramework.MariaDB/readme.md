# **Franz.Common.EntityFramework.MariaDB**

A library within the **Franz Framework** designed to extend **Entity Framework Core** support for **MariaDB** using the **Pomelo.EntityFrameworkCore.MySql** provider. This package simplifies the configuration and integration of MariaDB with EF Core in .NET applications.

---

## **Features**

- **MariaDB Integration**:
  - Leverages `Pomelo.EntityFrameworkCore.MySql` for robust MariaDB support.
- **Service Registration**:
  - `ServiceCollectionExtensions` to streamline dependency injection for MariaDB contexts.
- **Compatibility**:
  - Fully integrates with the **Franz.Common.EntityFramework** package for shared utilities and repositories.

---

## **Version Information**

- **Current Version**: 1.5.2
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.EntityFrameworkCore.Relational** (8.0.0): Provides relational database functionality for EF Core.
- **Pomelo.EntityFrameworkCore.MySql** (7.0.0): MariaDB provider for EF Core.
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
