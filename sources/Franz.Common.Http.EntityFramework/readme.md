Perfect 👍 — let’s extend your README so that **v1.6.1** highlights the new **NoSQL support** (MongoDB + Cosmos) in addition to the relational providers.

Here’s the refined version with the new changelog entry:

---

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

* **Multi-Database Provider Support** *(since 1.3.4, extended in 1.6.1)*:

  * Configure **MariaDB**, **Postgres**, **Oracle**, or **SQL Server** via `appsettings.json`.
  * **New in 1.6.1** → Polyglot persistence: support for **MongoDB** and **Azure Cosmos DB** as first-class NoSQL providers.

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

* **Current Version**: 1.6.1
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
* **Franz.Common.MongoDB** (new in 1.6.1)
* **Franz.Common.AzureCosmosDB** (new in 1.6.1)

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
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "FranzMongoDb"
  },
  "CosmosDb": {
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

### Version 1.6.1

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

👉 Do you want me to also generate a **Mermaid diagram** showing the persistence bootstrapper (`AddDatabase`) branching into Relational vs NoSQL providers, so the README visualizes the polyglot architecture?
