# **Franz.Common.EntityFramework**

A comprehensive library within the **Franz Framework**, designed to extend and simplify the integration of **Entity Framework Core** in .NET applications. This package provides additional features, abstractions, and utilities for managing relational and NoSQL databases, including support for **Cosmos DB** and **MongoDB**.

---

## **Features**

- **Database Configurations**:
  - Flexible configurations for **Cosmos DB** (`CosmosDBConfig`) and **MongoDB** (`MongoDBConfig`).
  - Centralized management of database options (`DatabaseOptions`).
- **Repositories**:
  - Abstractions for data persistence:
    - `ReadRepository`: For querying read-only data.
    - `AggregateRepository`: For managing aggregates in domain-driven design.
- **Multi-Database Context**:
  - Support for multiple database contexts (`DbContextMultiDatabase`).
- **Behaviors**:
  - `PersistenceBehavior` for managing transactional and persistence concerns.
- **Conversions**:
  - `EnumerationConverter`: Converts enumerations to database-friendly formats.
- **Extensions**:
  - `MediatorExtensions`: Extensions for MediatR.
  - `ModelBuilderExtensions`: Simplify model configuration.
  - `ServiceCollectionExtensions`: Streamlined dependency injection setup.

---

## **Version Information**

- **Current Version**: 1.2.62
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.EntityFrameworkCore** (8.0.0): Core Entity Framework functionality.
- **Microsoft.EntityFrameworkCore.Cosmos** (8.0.0): Cosmos DB provider for EF Core.
- **Microsoft.EntityFrameworkCore.Relational** (8.0.0): Relational database provider for EF Core.
- **Microsoft.Extensions.Configuration.Abstractions** (8.0.0): For configuration management.
- **Microsoft.Extensions.Hosting.Abstractions** (8.0.0): Hosting abstractions for dependency injection.
- **MongoDB.Driver** (2.22.0): MongoDB .NET driver for NoSQL database interaction.
- **Franz.Common.Business**: DDD and CQRS support.
- **Franz.Common.DependencyInjection**: Simplified DI patterns.
- **Franz.Common.Errors**: Centralized error management.
- **Franz.Common.MultiTenancy**: Multi-tenancy support for databases.
- **Franz.Common.Reflection**: Advanced reflection utilities.

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
dotnet add package Franz.Common.EntityFramework --version 1.2.62
```

---

## **Usage**

### **1. Configuring Database Contexts**

Define a context using `DbContextBase` or `DbContextMultiDatabase`:

```csharp
using Franz.Common.EntityFramework;

public class AppDbContext : DbContextBase
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
}
```

### **2. Cosmos DB Configuration**

Use the `CosmosDBConfig` class to configure Cosmos DB:

```csharp
using Franz.Common.EntityFramework.Configuration;

var cosmosConfig = new CosmosDBConfig
{
    Endpoint = "https://cosmosdb-endpoint.documents.azure.com",
    Key = "your-cosmos-key",
    DatabaseName = "MyDatabase"
};
```

### **3. MongoDB Configuration**

Configure MongoDB using `MongoDBConfig`:

```csharp
using Franz.Common.EntityFramework.Configuration;

var mongoConfig = new MongoDBConfig
{
    ConnectionString = "mongodb://localhost:27017",
    DatabaseName = "MyMongoDatabase"
};
```

### **4. Dependency Injection**

Use `ServiceCollectionExtensions` to register EF Core services:

```csharp
using Franz.Common.EntityFramework.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer("YourConnectionString"));

        services.AddCosmosDB(cosmosConfig);
        services.AddMongoDB(mongoConfig);
    }
}
```

### **5. Repositories**

Leverage `ReadRepository` or `AggregateRepository` for querying and persistence:

```csharp
using Franz.Common.EntityFramework.Repositories;

public class OrderRepository : ReadRepository<Order>
{
    public OrderRepository(AppDbContext context) : base(context) { }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.EntityFramework** library integrates seamlessly with:
- **Franz.Common.Business**: Enables DDD and CQRS patterns.
- **Franz.Common.DependencyInjection**: Simplifies DI setup for database services.
- **Franz.Common.Errors**: Provides standardized error handling.

Ensure these dependencies are installed to fully utilize the library.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team. If you have access, follow these steps:
1. Clone the repository.
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.2.62
- Added support for Cosmos DB and MongoDB configurations.
- Introduced `DbContextMultiDatabase` for multi-database environments.
- Added `PersistenceBehavior` for transactional support.
- Integrated `EnumerationConverter` for enum handling in databases.
- Streamlined DI with `ServiceCollectionExtensions`.

