# **Franz.Common.Http.EntityFramework**

A specialized library within the **Franz Framework** that integrates **Entity Framework Core** with **ASP.NET Core** applications. This package simplifies transactional handling, dependency injection, and middleware configurations, enhancing database operations in HTTP-based services.

---

## **Features**

- **Transactional Filters**:
  - `TransactionFilter` for managing database transactions seamlessly in API requests.
- **Service Registration**:
  - `ServiceCollectionExtensions` for registering database contexts and transactional filters.
- **Entity Framework Core Integration**:
  - Built-in support for relational database operations.
- **Modular Design**:
  - Compatible with other **Franz Framework** Entity Framework components, such as `Franz.Common.EntityFramework.MariaDB`.

---

## **Version Information**

- **Current Version**: 1.3.3
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.EntityFrameworkCore** (8.0.0): Core EF functionality for database access.
- **Microsoft.EntityFrameworkCore.Relational** (8.0.0): For relational database support.
- **Microsoft.AspNetCore.Mvc** (2.2.0): For integrating EF with ASP.NET Core MVC applications.
- **Franz.Common.DependencyInjection**: Simplifies dependency injection.
- **Franz.Common.EntityFramework.MariaDB**: Extends MariaDB-specific configurations and utilities.

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
dotnet add package Franz.Common.Http.EntityFramework  
```

---

## **Usage**

### **1. Register Database Contexts**

Use `ServiceCollectionExtensions` to register EF Core database contexts:

```csharp
using Franz.Common.Http.EntityFramework.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDatabaseContext<MyDbContext>(options =>
            options.UseSqlServer("YourConnectionString"));
    }
}
```

### **2. Enable Transaction Filters**

Apply the `TransactionFilter` to ensure transactional consistency:

```csharp
using Franz.Common.Http.EntityFramework.Transactions;

services.AddControllers(options =>
{
    options.Filters.Add<TransactionFilter>();
});
```

### **3. Middleware Configuration**

Integrate EF Core into the middleware pipeline:

```csharp
using Franz.Common.Http.EntityFramework.Extensions;

app.UseEntityFrameworkMiddleware();
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.EntityFramework** package integrates seamlessly with:
- **Franz.Common.EntityFramework**: Provides shared EF utilities and repository patterns.
- **Franz.Common.EntityFramework.MariaDB**: Offers MariaDB-specific configurations.
- **Franz.Common.DependencyInjection**: Simplifies service registration and dependency management.

Ensure these dependencies are installed to leverage the full capabilities of the library.

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