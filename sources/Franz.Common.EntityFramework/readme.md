

# **Franz.Common.EntityFramework**

A comprehensive library within the **Franz Framework**, designed to extend and simplify the integration of **Entity Framework Core** in .NET applications.
It provides clean abstractions, auditing, soft deletes, repositories, and seamless integration with Franz **Business** and **Mediator** packages.

---

## **Features**

* **Database Configurations*** 

  * Flexible options for **Cosmos DB** (`CosmosDBConfig`) and **MongoDB** (`MongoDBConfig`).
  * Centralized management of connection settings (`DatabaseOptions`).

* **Repositories** (**Separation of Entity and Aggregate Repositories**)

  * `EntityRepository<TEntity>`: CRUD operations for standalone entities.
  * `AggregateRepository<TAggregateRoot>`: For event-sourced aggregates.
  * `ReadRepository<T>`: Optimized read-only data access.

* **DbContextBase (🆕 canonical context)**

  * Built-in **auditing** (`CreatedBy`, `CreatedOn`, `LastModifiedBy`, `LastModifiedOn`).
  * **Soft deletes** (`IsDeleted`, `DeletedBy`, `DeletedOn`) with global query filters.
  * **Domain event dispatching** via Franz Mediator.
  * Removes the need for `SaveEntitiesAsync` – now everything happens in `SaveChangesAsync`.

* **Conversions**

  * `EnumerationConverter`: Easily map domain enumerations to EF Core-friendly values.

* **Extensions**

  * `ModelBuilderExtensions`: Simplify entity configuration.
  * `ServiceCollectionExtensions`: Wire up repositories and context with DI.

---

## **Version Information**

* **Current Version**:  2.0.1
* Part of the private **Franz Framework** ecosystem.

---

## **🆕 DbContextBase Example**

```csharp
public class AppDbContext : DbContextBase
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IDispatcher dispatcher, ICurrentUserService currentUser)
        : base(options, dispatcher, currentUser)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
```

### Key behaviors

* Automatically sets **CreatedBy / CreatedOn** when entities are added.
* Automatically sets **LastModifiedBy / LastModifiedOn** when entities are updated.
* Marks **IsDeleted / DeletedOn / DeletedBy** instead of physical deletes.
* Dispatches **domain events** after each save.

---

## **Repositories**

### 1️⃣ Entity Repository (CRUD Operations)

```csharp
public class OrderRepository : EntityRepository<AppDbContext, Order>
{
    public OrderRepository(AppDbContext dbContext) : base(dbContext) { }
}
```

✅ **Best for:**

* Entities that don’t require aggregate consistency.
* Simple CRUD operations.

---

### 2️⃣ Aggregate Repository (Event-Sourced Aggregates)

```csharp
public class ProductRepository : AggregateRepository<AppDbContext, Product, ProductEvent>
{
    public ProductRepository(AppDbContext dbContext, IEventStore eventStore)
        : base(dbContext, eventStore) { }
}
```

✅ **Best for:**

* Aggregates requiring event sourcing.
* Complex domain rules.
* Systems that replay history for consistency.

---

### 3️⃣ Choosing the Right Repository

| **Feature**                 | **EntityRepository** | **AggregateRepository**  |
| --------------------------- | -------------------- | ------------------------ |
| **Use Case**                | Standalone entities  | Event-sourced aggregates |
| **CRUD Support**            | ✅ Yes                | ❌ No (event-only)        |
| **Supports Event Sourcing** | ❌ No                 | ✅ Yes                    |
| **Direct Entity Access**    | ✅ Yes                | ❌ No (root only)         |

---

## **Auditing & Soft Deletes**

All entities deriving from Franz’s `Entity<TId>` automatically get:

* `CreatedOn`, `CreatedBy`
* `LastModifiedOn`, `LastModifiedBy`
* `IsDeleted`, `DeletedOn`, `DeletedBy`

### Example Entity

```csharp
public class Order : Entity<Guid>
{
    public string CustomerName { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }

    // Domain behavior...
}
```

EF Core automatically filters out `IsDeleted` entities via a global query filter.

---

## **Installation**

### From Private Azure Feed

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

Install the package:

```bash
dotnet add package Franz.Common.EntityFramework
```

---

## **Integration with Franz Framework**

* **Franz.Common.Business** → DDD & CQRS building blocks.
* **Franz.Common.Mediator** → Pipeline behaviors & domain event dispatching.
* **Franz.Common.Errors** → Standardized error handling.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal team.

1. Clone the repository: [GitHub - Franz.Common](https://github.com/bestacio89/Franz.Common/)
2. Create a feature branch.
3. Submit a pull request.

---

## **License**

Licensed under the **MIT License**.

---

## **Changelog**

### **1.6.15**
* Patch bump (package cleanup & docs).
* Fixed Compiletime Error in ReadRepository and IReadrepository (because of stupid c# generic constraints).

### **1.4.2**

* 🗑️ Removed `DbContextMultiDatabase` (use `DbContextBase` instead).
* 🗑️ Removed `SaveEntitiesAsync` (merged into `SaveChangesAsync`).
* ✅ Unified auditing, soft deletes, and domain events under `DbContextBase`.

### **1.4.1**

* Patch bump (package cleanup & docs).

### **1.4.0**

* Migrated to **C# 12** typing rules.
* Introduced auditing and soft delete handling in `DbContextBase`.
* Aligned `EnumerationConverter` with stricter typing constraints.

### **1.2.65**

* Split repositories into `EntityRepository` and `AggregateRepository`.
* Upgraded to .NET 9.

### **1.3**

* Upgraded to **.NET 9.0.8**.
* Added new features and improvements.
* Separated business from mediator concepts.
* Compatible with Franz mediator **and** MediatR.

---


### Version 1.6.20
- Updated to **.NET 10.0**


### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.