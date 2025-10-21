````markdown
# Franz.Common.Aras – v1.5.0 ??

**Franz.Common.Aras** integrates **ARAS Innovator** into the **Franz Framework** with clean, DDD-driven abstractions.  
It lets you treat ARAS as just another persistence provider — with **Entities**, **Aggregates**, **Unit of Work**, **Snapshots**, **Diagnostics**, and an **InMemory provider for testing**.

---

## ?? What’s New in v1.5.0

- ? **Concrete ARAS Innovator provider** (REST API)  
- ? **Fluent mapping layer** (field ? property, ignores, conversions)  
- ? **Unit of Work** (commit entities + aggregates atomically)  
- ? **Snapshotting** (optimize large aggregates, replay only post-snapshot events)  
- ? **Diagnostics decorators** (logging + OpenTelemetry tracing)  
- ? **InMemory provider** (unit testing without ARAS)  
- ? **AddArasInnovator bootstrapper** (EF-style DI registration)  
- ? **Full samples** for Entities, Aggregates, UoW, Testing, and Snapshots  
````
---
- **Current Version**: 1.6.17
--- 
## ?? Installation

```bash
dotnet add package Franz.Common.Aras
````

If you’re in a class library (not ASP.NET Core), also add:

```bash
dotnet add package Microsoft.Extensions.Http
```

---

## ?? Setup

Register ARAS Innovator with dependency injection:

```csharp
using Franz.Common.Aras.Innovator;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddArasInnovator(options =>
{
    options.BaseUrl = "https://aras.server/InnovatorServer";
    options.Database = "InnovatorSolutions";
    options.UserName = "admin";
    options.Password = "innovator";

    // Optional:
    options.UseDiagnostics = true;
    options.SnapshotFrequency = 50; // every 50 events
});
```

---

## ?? Entities (CRUD)

Entities are simple `Entity<Guid>` models.

```csharp
public class PartEntity : Entity<Guid>
{
    public string PartNumber { get; set; } = default!;
    public string Description { get; set; } = default!;
}
```

Define a repository:

```csharp
public class PartRepository : ArasEntityRepository<PartEntity>
{
    public PartRepository(IArasEntityContext context) : base(context) { }
}
```

Usage sample:

```csharp
var repo = provider.GetRequiredService<PartRepository>();

// Create
var part = new PartEntity { PartNumber = "PN-1000", Description = "Widget" };
await repo.AddAsync(part);

// Read
var fetched = await repo.GetByIdAsync(part.Id);
Console.WriteLine($"Fetched {fetched.PartNumber} - {fetched.Description}");

// Update
fetched.Description = "Updated Widget";
await repo.UpdateAsync(fetched);

// Delete
await repo.DeleteAsync(fetched.Id);
```

---

## ?? Aggregates (DDD + Events)

Aggregates inherit from `AggregateRoot<TEvent>` and model behavior with domain events.

```csharp
public record PartCreated(Guid PartId, string PartNumber) : BaseDomainEvent;

public class PartAggregate : AggregateRoot<PartCreated>
{
    public string PartNumber { get; private set; } = default!;

    public void Create(string number)
    {
        ApplyChange(new PartCreated(Id, number));
    }

    protected override void When(PartCreated e)
    {
        Id = e.PartId;
        PartNumber = e.PartNumber;
    }
}
```

Usage sample:

```csharp
var aggregates = provider.GetRequiredService<IArasAggregateContext>();

var partAgg = new PartAggregate();
partAgg.Create("PN-2000");

await aggregates.SaveAggregateAsync(partAgg);

// Load again (replay events or snapshot)
var loaded = await aggregates.GetAggregateAsync<PartAggregate, PartCreated>(partAgg.Id);

Console.WriteLine($"Loaded aggregate with PartNumber = {loaded.PartNumber}");
```

---

## ?? Unit of Work

Commit entities + aggregates in one go:

```csharp
using var uow = provider.GetRequiredService<IArasUnitOfWork>();

// Entities
uow.Entities.Add(new PartEntity { PartNumber = "PN-3000", Description = "Bolt" });

// Aggregates
var partAgg = new PartAggregate();
partAgg.Create("PN-4000");
uow.Aggregates.TrackAggregate(partAgg);

// Commit atomically
await uow.CommitAsync();
Console.WriteLine("UoW committed successfully");
```

Rollback clears tracked changes:

```csharp
await uow.RollbackAsync();
Console.WriteLine("UoW rolled back");
```

---

## ?? InMemory Provider (Testing)

Use the in-memory contexts without ARAS server:

```csharp
services.AddInMemoryAras();

var context = provider.GetRequiredService<IArasEntityContext>();

await context.SaveEntityAsync(new PartEntity { PartNumber = "TEST-1" });

var parts = await context.QueryEntitiesAsync<PartEntity>("all");

foreach (var part in parts)
    Console.WriteLine($"InMemory part: {part.PartNumber}");
```

Aggregate testing:

```csharp
var aggregates = provider.GetRequiredService<IArasAggregateContext>();

var agg = new PartAggregate();
agg.Create("TEST-AGG");
await aggregates.SaveAggregateAsync(agg);

var reloaded = await aggregates.GetAggregateAsync<PartAggregate, PartCreated>(agg.Id);
Console.WriteLine($"Reloaded aggregate: {reloaded.PartNumber}");
```

---

## ?? Snapshots (Performance)

Aggregates are snapshotted automatically every *N* events (default 50):

```csharp
var agg = await aggregates.GetAggregateAsync<PartAggregate, PartCreated>(id);
// Replays only events after last snapshot
```

You can configure snapshot frequency in `ArasInnovatorOptions`:

```csharp
options.SnapshotFrequency = 25;
```

---

## ?? Diagnostics

Enable structured logging + tracing with decorators:

```csharp
services.AddLogging(b => b.AddConsole());
services.AddOpenTelemetryTracing();

services.Decorate<IArasEntityContext, DiagnosticEntityContextDecorator>();
services.Decorate<IArasAggregateContext, DiagnosticAggregateContextDecorator>();
```

You’ll get logs for:

* Entity queries, saves, deletes
* Aggregate tracking, saving, committing
* Event publishing
* Snapshot operations

Sample log:

```
[INF] Querying entities of type PartEntity
[INF] Saving aggregate PartAggregate/PN-2000 with 1 new events
[INF] Published event PartCreated { PartId = ..., PartNumber = "PN-2000" }
```

---

## ? Summary

* **Entities** ? simple CRUD
* **Aggregates** ? DDD + event sourcing
* **Unit of Work** ? atomic batch commit
* **InMemory** ? testing without ARAS
* **Snapshots** ? scalable performance
* **Diagnostics** ? enterprise observability
* **Bootstrapper** ? one-liner service registration

---

## ?? Samples

This repo includes a `samples/` folder with ready-to-run console apps:

* `Sample.Entities` ? CRUD with PartEntity
* `Sample.Aggregates` ? Domain events with PartAggregate
* `Sample.UoW` ? Entities + aggregates in one transaction
* `Sample.InMemory` ? Tests without ARAS
* `Sample.Diagnostics` ? Logs + tracing

---

Franz.Common.Aras v1.5.0 — **bringing ARAS into the DDD world.**

```

---

