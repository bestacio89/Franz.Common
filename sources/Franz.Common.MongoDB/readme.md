# Franz.Common.MongoDB

> MongoDB context, tenant-aware repository, aggregate repository, and messaging-store integrations.

## Overview

The package provides a MongoDB context registration helper, native tenant-scoped repositories, and Mongo-backed outbox/inbox storage. It does not provide an EF Core provider.

## Installation

```bash
dotnet add package Franz.Common.MongoDB
```

## Register a Mongo context

The registration helper reads `MongoDb:ConnectionString` and `MongoDb:DatabaseName`, registers `IMongoClient` as a singleton, registers `IMongoDatabase` and the concrete context as scoped services:

```csharp
using Franz.Common.MongoDB.Extensions;
using Franz.Common.MongoDB.Configuration;

var mongoSection = builder.Configuration.GetSection("MongoDb");
builder.Services.Configure<MongoDBConfig>(options =>
{
    options.ConnectionString = mongoSection["ConnectionString"];
    options.Database = mongoSection["DatabaseName"];
    options.Collection = mongoSection["Collection"];
});
builder.Services.AddMongoDbContext<ApplicationMongoContext>(builder.Configuration);
```

The explicit `Configure<MongoDBConfig>` mapping is required when the context derives from `MongoDbContext`, because that base context consumes `IOptions<MongoDBConfig>` and reads its `Database` property. `AddMongoDbContext` reads `MongoDb:ConnectionString` and `MongoDb:DatabaseName` for client/database registration but does not bind `MongoDBConfig`; the options mapping must translate `DatabaseName` to `Database` as shown.

```json
{
  "MongoDb": {
    "ConnectionString": "<secret>",
    "DatabaseName": "application",
    "Collection": "customers"
  }
}
```

## Tenant-aware native repositories

For documents implementing `IMongoTenantEntity`:

```csharp
builder.Services.AddMongoTenantRepository<CustomerDocument>("customers");
```

The registration adds the concrete `MongoTenantRepository<CustomerDocument>` to DI; it does not register a separate repository interface. The repository requires `IMongoDatabase` and a scoped `ITenantExecutionContextAccessor`, and every read/mutation includes the validated tenant ID in the MongoDB filter. Inserts stamp an empty `TenantId` from the current context and reject cross-tenant ownership.

For event-sourced aggregate repositories:

```csharp
builder.Services.AddMongoTenantAggregateRepository<OrderAggregate, OrderEvent>();
```

This registration adds the concrete `MongoAggregateRepository<TAggregate, TEvent>` to DI and requires the scoped tenant execution context plus Mediator dispatcher. The class implements `IAggregateRepository<TAggregate, TEvent>`, but the helper registers only the concrete type; add an explicit `IAggregateRepository<TAggregate, TEvent>` mapping if the application resolves the repository through that abstraction. It is an event-store repository that persists and replays tenant-filtered domain events.

## Messaging stores

```csharp
builder.Services.AddMongoMessageStore(
    connectionString: mongoConnectionString,
    dbName: "messaging",
    outboxCollectionName: "OutboxMessages",
    deadLetterCollectionName: "DeadLetterMessages");
```

The helper registers Mongo client/database instances, `IMessageStore`, and `IInboxStore` as singletons. It takes the connection string and database name directly; configure those values explicitly at the registration call. This messaging-store registration is not tenant-scoped by the helper.

Use a secret manager for connection strings. Do not reuse tenant-scoped DbContexts or repository scopes across tenant execution boundaries.

## License

MIT. See the repository [LICENSE](../../LICENSE).
