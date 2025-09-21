
---

# **Franz.Common.Http.EntityFramework**

A specialized library within the **Franz Framework** that integrates **Entity Framework Core** with **ASP.NET Core** applications. This package simplifies transactional handling, dependency injection, and middleware configurations, enhancing database operations in HTTP-based services.

---

## **Features**

* **Transactional Filters**:

  * `TransactionFilter` for managing database transactions seamlessly in API requests.
* **Service Registration**:

  * `ServiceCollectionExtensions` for registering database contexts and transactional filters.
* **Entity Framework Core Integration**:

  * Built-in support for relational database operations.
* **Multi-Database Provider Support** *(new in 1.3.4)*:

  * Easily configure **MariaDB**, **Postgres**, **Oracle**, or **SQL Server** via `appsettings.json`.
* **Modular Design**:

  * Compatible with other **Franz Framework** Entity Framework components, such as `Franz.Common.EntityFramework.MariaDB`, `Franz.Common.EntityFramework.Postgres`, etc.

---

## **Version Information**

- **Current Version**: 1.5.3

* Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:

* **Microsoft.EntityFrameworkCore** (8.0.0)
* **Microsoft.EntityFrameworkCore.Relational** (8.0.0)
* **Microsoft.AspNetCore.Mvc** (2.2.0)
* **Franz.Common.DependencyInjection**
* **Franz.Common.EntityFramework.MariaDB**
* **Franz.Common.EntityFramework.Postgres**
* **Franz.Common.EntityFramework.Oracle**
* **Franz.Common.EntityFramework.SQLServer**

---

## **Installation**

### **From Private Azure Feed**

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

### **1. Configure Provider in appsettings.json**

```json
{
  "Database": {
    "Provider": "Postgres",
    "ConnectionString": "Host=localhost;Database=mydb;Username=myuser;Password=mypass"
  }
}
```

Supported providers: `MariaDb`, `Postgres`, `Oracle`, `SqlServer`.

---

### **2. Register Database Context**

```csharp
builder.Services.AddDatabase<MyDbContext>(builder.Environment, builder.Configuration);
```

---

### **3. Enable Transaction Filters**

Automatically applied by default, but can be explicitly added:

```csharp
using Franz.Common.Http.EntityFramework.Transactions;

services.AddControllers(options =>
{
    options.Filters.Add<TransactionFilter>();
});
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.EntityFramework** package integrates seamlessly with:

* **Franz.Common.EntityFramework**
* **Franz.Common.EntityFramework.MariaDB**
* **Franz.Common.EntityFramework.Postgres**
* **Franz.Common.EntityFramework.Oracle**
* **Franz.Common.EntityFramework.SQLServer**
* **Franz.Common.DependencyInjection**

---

## **Changelog**

### Version 1.3.4

* Added **multi-database provider support** (MariaDB, Postgres, Oracle, SQL Server).
* Provider selection now handled via `appsettings.json` (`Database:Provider`).
* Simplified registration: `AddDatabase<TDbContext>(env, config)`.

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.2.65
- Upgrade version to .net 9
---

⚡ This way your README reflects the new **multi-db capability** front and center.

Do you want me to also draft a **configuration schema section** (like a JSON schema snippet or table) that documents all possible keys for `Database` (Provider, ConnectionString, maybe future options like Schema, PoolSize, MigrationsAssembly)?





