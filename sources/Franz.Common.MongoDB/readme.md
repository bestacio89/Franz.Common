# Franz.Common.MongoDB

> MongoDB persistence integration for generic repositories, tenant-scoped document access, event streams, and messaging inbox/outbox stores.

## Overview

The package contains separate generic, tenant-aware, event-store, and messaging persistence paths. Only the tenant-specific repositories apply tenant identity to every MongoDB operation. Do not treat raw collection access or generic repositories as tenant-isolated.

## Installation

~~~bash
dotnet add package Franz.Common.MongoDB
~~~

## Tenant-owned documents

Tenant-owned documents implement IMongoTenantEntity, which extends ITenantEntity and adds a string Id. Register a tenant-scoped repository after registering IMongoDatabase and ITenantExecutionContextAccessor:

~~~csharp
using Franz.Common.MongoDB.Extensions;

services.AddMongoTenantRepository<OrderDocument>("orders");
~~~

The resulting MongoTenantRepository<OrderDocument> is scoped. It applies the current validated tenant ID to server-side filters for reads, updates, deletes, atomic updates, and bulk updates. Inserts assign the current tenant when TenantId is empty and reject a document already assigned to another tenant.

The repository exposes GetByIdAsync, GetAllAsync, FindAsync, AddAsync, AddRangeAsync, UpdateAsync, DeleteAsync, UpdateOneAsync, UpdateManyAsync, BulkUpdateAsync, and ExecuteInTransactionAsync. Its tenant identity index helper creates a compound tenant/document identity index; invoke index creation during deployment or migration, not per request.

MongoDB transactions require a replica set or sharded cluster. Use the transaction API only when the deployment supports transactions.

## Tenant-aware event store

Register the tenant-aware aggregate repository with:

~~~csharp
using Franz.Common.MongoDB.Extensions;

services.AddMongoTenantAggregateRepository<OrderAggregate, OrderEvent>();
~~~

This registration requires IMongoDatabase, IDispatcher, and ITenantExecutionContextAccessor. Event reads and writes are constrained by the validated tenant identity. The repository stamps persisted events from the execution context, not from event metadata.

## Generic repository boundary

MongoRepository<TEntity> is a legacy generic CRUD repository. It filters by document ID only and does not enforce tenant isolation. Do not use it for tenant-owned documents. Use MongoTenantRepository<TEntity> instead.

The older non-tenant Mongo aggregate repository path can operate only on legacy/non-tenant events when no tenant accessor is supplied. Do not treat that path as tenant isolation.

## Messaging inbox/outbox

AddMongoMessageStore registers the generic MongoMessageStore and MongoInboxStore:

~~~csharp
using Franz.Common.MongoDB.Extensions;

services.AddMongoMessageStore(
    connectionString: configuration["MongoDb:ConnectionString"]!,
    dbName: configuration["MongoDb:DatabaseName"]!,
    outboxCollectionName: "OutboxMessages",
    deadLetterCollectionName: "DeadLetterMessages");
~~~

This method registers singleton Mongo client/database and generic inbox/outbox stores. These stores are not automatically tenant-filtered. If an application requires tenant isolation for messaging persistence, it must use a separately designed tenant-aware store contract; do not assume the tenant document repository protects these collections.

## Context registration note

AddMongoDbContext<TContext> reads MongoDb:ConnectionString and MongoDb:DatabaseName, registers IMongoClient, IMongoDatabase, and the context. MongoDbContext itself also consumes IOptions<MongoDBConfig> and reads its Database property. The extension does not bind MongoDBConfig, so applications using this base context must register that options object explicitly and keep its Database value aligned with MongoDb:DatabaseName. This registration contract should be reviewed before relying on it as a one-call setup.

## Operational requirements

- Keep MongoClient long-lived; the driver client is thread-safe.
- Use a scoped tenant execution context for tenant repositories.
- Create required compound indexes as part of deployment/index migration.
- Do not bypass tenant filters with raw collection access.
- Do not claim that generic inbox/outbox stores are tenant-isolated.

## License

MIT. See the repository [LICENSE](../../LICENSE).
