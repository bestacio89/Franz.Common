# **Franz.Common.EntityFramework**  

A comprehensive library within the **Franz Framework**, designed to extend and simplify the integration of **Entity Framework Core** in .NET applications. This package provides additional features, abstractions, and utilities for managing relational and NoSQL databases, including support for **Cosmos DB** and **MongoDB**.

---

## **Features**  

- **Database Configurations**:  
  - Flexible configurations for **Cosmos DB** (`CosmosDBConfig`) and **MongoDB** (`MongoDBConfig`).  
  - Centralized management of database options (`DatabaseOptions`).  
- **Repositories** (**🆕 Separation of Entity and Aggregate Repositories**):  
  - `EntityRepository<TEntity>`: CRUD operations for standalone entities.  
  - `AggregateRepository<TAggregateRoot>`: Manages aggregates using event sourcing.  
  - `ReadRepository<T>`: Read-only data access.  
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

- - **Current Version**: 1.3.14
- Part of the private **Franz Framework** ecosystem.  

---

## **🆕 Separation of Entity and Aggregate Repositories**  

> **As of version 1.2.65**, repositories have been split into:  
> - **`EntityRepository<TEntity>`** → Used for **CRUD-based** operations.  
> - **`AggregateRepository<TAggregateRoot>`** → Used for **event-sourced aggregates**.  

### **1️⃣ Entity Repository (CRUD Operations)**  
For simple **standalone entities** that don’t require event sourcing:  
```csharp
public class EntityRepository<TDbContext, TEntity> : IEntityRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : class, IEntity
{
    protected readonly TDbContext DbContext;

    public EntityRepository(TDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<TEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<TEntity>().FindAsync(new object?[] { id }, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<TEntity>().Update(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<TEntity>().Remove(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
```
✅ **Best for:**  
- Entities that **don’t require consistency across multiple objects**.  
- Standard **CRUD operations** (Create, Read, Update, Delete).  

---

### **2️⃣ Aggregate Repository (Event-Sourced Aggregates)**  
For managing **aggregates** using **event sourcing**:  
```csharp
public abstract class AggregateRepository<TDbContext, TAggregateRoot, TEvent>
    where TDbContext : DbContext
    where TAggregateRoot : EventSourcedAggregateRoot<TEvent>
    where TEvent : BaseEvent
{
    protected readonly TDbContext DbContext;
    private readonly IEventStore _eventStore;

    public AggregateRepository(TDbContext dbContext, IEventStore eventStore)
    {
        DbContext = dbContext;
        _eventStore = eventStore;
    }

    public async Task<TAggregateRoot> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aggregate = await DbContext.Set<TAggregateRoot>().FindAsync(new object[] { id }, cancellationToken);
        if (aggregate == null) return null;

        var events = await _eventStore.GetEventsByAggregateIdAsync(id);
        aggregate.ReplayEvents(events);

        return aggregate;
    }

    public async Task SaveAsync(TAggregateRoot aggregate, CancellationToken cancellationToken = default)
    {
        var uncommittedChanges = aggregate.GetUncommittedChanges();

        foreach (var @event in uncommittedChanges)
        {
            await _eventStore.SaveEventAsync(@event);
        }

        aggregate.MarkChangesAsCommitted();
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
```
✅ **Best for:**  
- Aggregates that require **transactional consistency**.  
- Systems using **event sourcing**.  
- Complex **business rules that enforce domain logic**.  

---

### **🆕 3️⃣ Choosing the Right Repository**  

| **Feature** | **EntityRepository** | **AggregateRepository** |
|------------|---------------------|------------------------|
| **Use Case** | Standalone entities | Event-sourced aggregates |
| **CRUD Support?** | ✅ Yes | ❌ No (modifications via events) |
| **Supports Event Sourcing?** | ❌ No | ✅ Yes |
| **Direct Entity Access?** | ✅ Yes | ❌ No (changes happen via root) |

---

## **Usage Examples**  

### **1️⃣ CRUD-Based Repository (For Entities)**  
```csharp
public class OrderRepository : EntityRepository<AppDbContext, Order>
{
    public OrderRepository(AppDbContext dbContext) : base(dbContext) { }
}
```

### **2️⃣ Event-Sourced Repository (For Aggregates)**  
```csharp
public class ProductRepository : AggregateRepository<AppDbContext, Product, ProductEvent>
{
    public ProductRepository(AppDbContext dbContext, IEventStore eventStore)
        : base(dbContext, eventStore) { }
}
```

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
dotnet add package Franz.Common.EntityFramework  
```

---

## **Integration with Franz Framework**  
The **Franz.Common.EntityFramework** library integrates seamlessly with:  
- **Franz.Common.Business**: Enables **DDD and CQRS** patterns.  
- **Franz.Common.DependencyInjection**: Simplifies **DI setup** for repositories.  
- **Franz.Common.Errors**: Provides **standardized error handling**.  

---

## **Contributing**  
This package is part of a private framework. Contributions are limited to the internal development team.  
1. Clone the repository. @ [GitHub - Franz.Common](https://github.com/bestacio89/Franz.Common/)  
2. Create a feature branch.  
3. Submit a pull request for review.  

---

## **License**  
This library is licensed under the MIT License.

---

## **Changelog**  
### **Version 1.2.65**  
- **Introduced explicit separation between Entity and Aggregate Repositories.**  
- **Implemented event-based aggregate repository structure.**  
- **Upgraded to .NET 9**. 

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

---
