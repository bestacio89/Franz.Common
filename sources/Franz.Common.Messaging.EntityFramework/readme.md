# **Franz.Common.Messaging.EntityFramework**

A specialized library within the **Franz Framework** designed to integrate **Entity Framework Core** with messaging workflows.
This package ensures **transactional consistency** for outbox patterns in relational databases, while also supporting **polyglot persistence** by delegating messaging storage to MongoDB or Azure Cosmos DB when configured.

---

## **Features**

* **Transactional Filters**

  * `TransactionFilter` to manage database transactions in messaging contexts, ensuring consistency and rollback.

* **Service Registration**

  * `ServiceCollectionExtensions` to simplify the integration of messaging and persistence services.

* **Entity Framework Core Messaging**

  * Native support for relational outbox patterns when using **EF Core** (MariaDB, Postgres, Oracle, SQL Server).

* **Polyglot Messaging Support** *(new in 1.6.2)*

  * Messaging outbox/dead-letter can also be stored in **MongoDB** or **Azure Cosmos DB** via unified configuration.
  * Clean abstraction with `IMessageStore`, so your messaging workflow does not change regardless of provider.

---

## **Version Information**

* **Current Version**: 1.7.6
* Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:

* **Microsoft.EntityFrameworkCore** (8.0.0) – Core EF functionality for database access
* **Microsoft.EntityFrameworkCore.Relational** (8.0.0) – Relational database support
* **Franz.Common.EntityFramework.MariaDB** – MariaDB provider bootstrap
* **Franz.Common.Messaging.Hosting** – Hosting + transaction coordination
* **Franz.Common.MongoDB** *(new in 1.6.2)* – MongoDB messaging provider
* **Franz.Common.AzureCosmosDB** *(new in 1.6.2)* – Cosmos DB messaging provider

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
dotnet add package Franz.Common.Messaging.EntityFramework  
```

---

## **Usage**

### **1. Configure Messaging Provider**

```json
{
  "Messaging": {
    "Provider": "Postgres" // Options: MariaDb, Postgres, Oracle, SqlServer, Mongo, Cosmos
  },
  "Database": {
    "ConnectionString": "Host=localhost;Database=franz;Username=sa;Password=pass"
  },
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "FranzOutbox"
  },
  "CosmosDb": {
    "ConnectionString": "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key;",
    "DatabaseName": "FranzCosmosOutbox"
  }
}
```

---

### **2. Register Messaging Outbox**

```csharp
using Franz.Common.Messaging.EntityFramework.Extensions;

builder.Services.AddMessageStore<MyDbContext>(builder.Configuration);
```

* If `Provider` is `Postgres`, `MariaDb`, `Oracle`, or `SqlServer` → EF-based outbox via `DbContext`.
* If `Provider` is `Mongo` → outbox/dead-letter stored in Mongo collections.
* If `Provider` is `Cosmos` → outbox/dead-letter stored in Cosmos containers.

---

### **3. Transaction Filters (Relational Only)**

When using relational databases, ensure transaction consistency with filters:

```csharp
using Franz.Common.Messaging.EntityFramework.Transactions;

services.AddControllers(options =>
{
    options.Filters.Add<TransactionFilter>();
});
```

---

### **4. Messaging Service Example**

```csharp
public class MessagingService
{
    private readonly IMessageStore _messageStore;

    public MessagingService(IMessageStore messageStore)
    {
        _messageStore = messageStore;
    }

    public async Task PublishAsync(Message message)
    {
        await _messageStore.SaveAsync(message);
    }
}
```

The code above works the same whether your provider is EF, Mongo, or Cosmos.

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.EntityFramework** package integrates seamlessly with:

* **Franz.Common.EntityFramework** (base EF abstractions)
* **Franz.Common.EntityFramework.MariaDB**
* **Franz.Common.EntityFramework.Postgres**
* **Franz.Common.EntityFramework.Oracle**
* **Franz.Common.EntityFramework.SQLServer**
* **Franz.Common.MongoDB** *(NoSQL messaging)*
* **Franz.Common.AzureCosmosDB** *(NoSQL messaging)*
* **Franz.Common.DependencyInjection**

---

## **Changelog**

### Version 1.6.2

* Extended **messaging persistence** beyond EF: added support for **MongoDB** and **Azure Cosmos DB**.
* Unified **config-driven provider selection** for messaging outbox/dead-letter (`Messaging:Provider`).
* EF relational providers still supported via `DbContextBase` + `StoredMessage` entity.
* Mongo & Cosmos backed by dedicated `IMessageStore` implementations.
* All providers expose the same abstraction for **transparent messaging workflows**.

### Version 1.3

* Upgraded to **.NET 9.0.8**
* Added new features and improvements
* Separated **business concepts** from **mediator concepts**
* Now compatible with both the in-house mediator and MediatR

### Version 1.2.65

* Upgraded version to **.NET 9**

* ### Version 1.6.20
- Updated to **.NET 10.0**
- Removal and Deprecation of oracle support due to Oracle's shift away from EF Core.
---

