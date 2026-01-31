# **Franz.Common.Http.EntityFramework**

A specialized library within the **Franz Framework** that integrates **Entity Framework Core** with **ASP.NET Core** applications.
This package simplifies transactional handling, dependency injection, and middleware configurations, enhancing database operations in HTTP-based services.

---

## **Features**

* **Transactional Filters**:

  * `TransactionFilter` for managing database transactions seamlessly in API requests.

* **Service Registration**:

  * `ServiceCollectionExtensions` for registering database contexts and transactional filters.

* **Entity Framework Core Integration**:

  * Built-in support for relational database operations.

* **Multi-Database Provider Support** *(since 1.3.4, extended in 1.6.2 & 1.6.3)*:

  * Configure **MariaDB**, **Postgres**, **Oracle**, or **SQL Server** via `appsettings.json`.
  * **Since 1.6.2** → Polyglot persistence: support for **MongoDB** and **Azure Cosmos DB** as first-class NoSQL providers.
  * **New in 1.6.3** → Environment-aware multi-database registration with provider validation, preventing silent misconfigurations.

* **Modular Design**:

  * Compatible with other **Franz Framework** persistence components, such as:

    * `Franz.Common.EntityFramework.MariaDB`
    * `Franz.Common.EntityFramework.Postgres`
    * `Franz.Common.EntityFramework.Oracle`
    * `Franz.Common.EntityFramework.SQLServer`
    * `Franz.Common.MongoDB`
    * `Franz.Common.AzureCosmosDB`

---

## **Version Information**

* **Current Version**: 1.7.7
  → Adds **environment-aware validation**, stronger governance for multi-database setups, and provider-context alignment.
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
* **Franz.Common.MongoDB** (since 1.6.2)
* **Franz.Common.AzureCosmosDB** (since 1.6.2)

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
  },
  "MongoDb": {
    "Provider": "Mongo",
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "FranzMongoDb"
  },
  "CosmosDb": {
    "Provider": "Cosmos",
    "ConnectionString": "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key;",
    "DatabaseName": "FranzCosmosDb"
  }
}
```

Supported providers: `MariaDb`, `Postgres`, `Oracle`, `SqlServer`, `Mongo`, `Cosmos`.

---

### **2. Register Database Context**

```csharp
builder.Services.AddDatabase<MyDbContext>(builder.Environment, builder.Configuration);
```

For **multiple contexts** (polyglot persistence):

```csharp
builder.Services.RegisterDatabaseForContext<MyRelationalDbContext>(builder.Configuration.GetSection("Database"));
builder.Services.RegisterDatabaseForContext<MyMongoDbContext>(builder.Configuration.GetSection("MongoDb"));
builder.Services.RegisterDatabaseForContext<MyCosmosStore>(builder.Configuration.GetSection("CosmosDb"));
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
* **Franz.Common.MongoDB**
* **Franz.Common.AzureCosmosDB**
* **Franz.Common.DependencyInjection**

---

## **Changelog**

### Version 1.6.20
- Updated to **.NET 10.0**
- Deprecation of Oracle support due to Oracle's shift away from EF Core.

### Version 1.6.3

* **Environment-aware validation** added to `AddDatabase<TDbContext>` and `RegisterDatabaseForContext<TContext>`:

  * Enforces correct provider/context alignment for **relational (EF)**, **MongoDB**, and **CosmosDB** contexts.
  * Prevents silent misconfigurations (wrong provider/context combinations now throw explicit exceptions).
  * Guards against hardcoded connection strings — all values must come from `appsettings.{Environment}.json`.
* **Governance baked in for relational DBs**:

  * Automatic registration of **transactions per HTTP call**, **generic repositories**, and **behaviors**.
* Extended **multi-database orchestration**:

  * Support for mixing multiple contexts (`MariaDB`, `Postgres`, `Oracle`, `SQLServer`, `MongoDB`, `CosmosDB`) within the same app.
  * Polyglot persistence scenarios are now governed by clear, opinionated rules.

### Version 1.6.2

* Extended **multi-database provider support** to include **NoSQL** providers:

  * Added **MongoDB** support via `Franz.Common.MongoDB`.
  * Added **Azure Cosmos DB** support via `Franz.Common.AzureCosmosDB`.
* `AddDatabase<TDbContext>` now supports both relational and document database providers via config.
* Introduced `CosmosDBMessageStore` and `MongoMessageStore` to unify outbox & dead-letter messaging in NoSQL providers.
* Full alignment with Franz polyglot persistence philosophy.

### Version 1.3.4

* Added **multi-database provider support** (MariaDB, Postgres, Oracle, SQL Server).
* Provider selection now handled via `appsettings.json` (`Database:Provider`).
* Simplified registration: `AddDatabase<TDbContext>(env, config)`.

### Version 1.3

* Upgraded to **.NET 9.0.8**
* Added **new features and improvements**
* Separated **business concepts** from **mediator concepts**
* Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.2.65

* Upgraded version to **.NET 9**

---
