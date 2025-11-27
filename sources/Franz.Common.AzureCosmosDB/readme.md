# **Franz.Common.AzureCosmosDB**

A specialized persistence library within the **Franz Framework**, providing seamless integration with **Azure Cosmos DB**.
This package extends the polyglot persistence philosophy of the framework by enabling **NoSQL storage**, **outbox pattern messaging**, and **dead-letter handling** alongside existing SQL and Mongo providers.

---

## **Features**

* **Cosmos DB Bootstrapping**:

  * `ServiceCollectionExtensions` to configure and register Cosmos DB dependencies via configuration.
  * Automatic registration of `CosmosClient`, `Database`, and container-level resources.
* **Outbox & Dead-Letter Messaging**:

  * `CosmosDBMessageStore` implements `IMessageStore` from **Franz.Common.Messaging.Storage**, enabling reliable message persistence, retries, and dead-letter handling.
* **Repository Support**:

  * Generic repository pattern (`ICosmosRepository<T>`) for CRUD operations.
  * Partition key awareness and container management out-of-the-box.
* **Config-Driven**:

  * Centralized setup through `appsettings.json` (`CosmosDb:ConnectionString`, `CosmosDb:DatabaseName`).
* **Polyglot Persistence**:

  * Aligns with the same philosophy as `Franz.Common.EntityFramework` and `Franz.Common.MongoDB`, allowing developers to swap or combine providers effortlessly.

---

## **Version Information**

* **Current Version**: 1.6.21
* Part of the private **Franz Framework** ecosystem.
* Extended capacities: Cosmos DB support introduced in 1.6.2.

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
dotnet add package Franz.Common.AzureCosmosDB
```

---

## **Usage**

### **1. Configure Cosmos Database**

In `appsettings.json`:

```json
{
  "CosmosDb": {
    "ConnectionString": "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key;",
    "DatabaseName": "FranzAppDb"
  }
}
```

### **2. Register Cosmos in DI**

```csharp
using Franz.Common.Cosmos.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCosmosDatabase(Configuration);  
        services.AddCosmosMessageStore(
            Configuration["CosmosDb:ConnectionString"],
            Configuration["CosmosDb:DatabaseName"]);
    }
}
```

---

### **3. Outbox & Dead-Letter Messaging**

```csharp
public class MyService
{
    private readonly IMessageStore _messageStore;

    public MyService(IMessageStore messageStore)
    {
        _messageStore = messageStore;
    }

    public async Task SendMessageAsync()
    {
        var message = new Message("Hello from Franz!");
        await _messageStore.SaveAsync(message);
    }
}
```

---

## **Dependencies**

* **Franz.Common.Messaging**: Contracts (`Message`, `StoredMessage`)
* **Franz.Common.Messaging.Storage**: `IMessageStore` abstraction
* **Microsoft.Azure.Cosmos**: Cosmos DB SDK

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team.

1. Clone the repository @ [https://github.com/bestacio89/Franz.Common/](https://github.com/bestacio89/Franz.Common/)
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

Licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.6.2

* Introduced **Azure Cosmos DB integration**.
* Added `CosmosDBMessageStore` implementing `IMessageStore`.
* Added `ICosmosRepository<T>` for generic repository patterns.
* Enabled **Outbox / Dead-Letter pattern** in Cosmos DB.
* Extended polyglot persistence bootstrapper to include Cosmos alongside SQL and Mongo.

### Version 1.6.20
- Updated to **.NET 10.0**