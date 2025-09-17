# **Franz.Common.Messaging.EntityFramework**

A specialized library within the **Franz Framework** designed to integrate **Entity Framework Core** with messaging workflows. This package provides tools for managing database transactions seamlessly in messaging scenarios, ensuring consistency across distributed systems.

---

## **Features**

- **Transactional Filters**:
  - `TransactionFilter` to manage database transactions in messaging contexts, ensuring consistency and rollback capabilities.
- **Service Registration**:
  - `ServiceCollectionExtensions` to simplify the integration of messaging and Entity Framework Core services.
- **Entity Framework Core Integration**:
  - Built-in support for relational databases through **Entity Framework Core**.

---

## **Version Information**

- **Current Version**:  1.3.10
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.EntityFrameworkCore** (8.0.0): Core EF functionality for database access.
- **Microsoft.EntityFrameworkCore.Relational** (8.0.0): Provides relational database support.
- **Franz.Common.EntityFramework.MariaDB**: Extends MariaDB-specific configurations.
- **Franz.Common.Messaging.Hosting**: Enables hosting configurations for messaging.

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
dotnet add package Franz.Common.Messaging.EntityFramework  
```

---

## **Usage**

### **1. Register Database Contexts**

Use `ServiceCollectionExtensions` to register EF Core database contexts and messaging services:

```csharp
using Franz.Common.Messaging.EntityFramework.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMessagingWithEntityFramework<MyDbContext>(options =>
            options.UseSqlServer("YourConnectionString"));
    }
}
```

### **2. Enable Transaction Filters**

Apply the `TransactionFilter` to ensure transactional consistency in messaging workflows:

```csharp
using Franz.Common.Messaging.EntityFramework.Transactions;

services.AddControllers(options =>
{
    options.Filters.Add<TransactionFilter>();
});
```

### **3. Integrate Messaging with Transactions**

Ensure that all messaging operations are wrapped within a transaction, providing consistency across distributed systems:

```csharp
using Franz.Common.Messaging.EntityFramework.Transactions;

public class MessagingService
{
    private readonly TransactionFilter _transactionFilter;

    public MessagingService(TransactionFilter transactionFilter)
    {
        _transactionFilter = transactionFilter;
    }

    public async Task ProcessMessageAsync()
    {
        await _transactionFilter.BeginTransactionAsync();
        // Perform messaging operations
        await _transactionFilter.CommitTransactionAsync();
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.EntityFramework** package integrates seamlessly with:
- **Franz.Common.EntityFramework**: Core utilities for EF Core integration.
- **Franz.Common.EntityFramework.MariaDB**: Provides additional configurations for MariaDB.
- **Franz.Common.Messaging.Hosting**: Enables hosting and transaction management for messaging workflows.

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