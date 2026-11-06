# Franz.Common.AzureCosmosDB

> Azure Cosmos DB EF Core and native SDK integration for Franz persistence and messaging.

## Overview

This package provides Cosmos-specific DbContext registration, EF entity repositories, native tenant-partitioned document repositories, and Cosmos-backed messaging stores. It is separate from the relational provider adapters.

## Installation

```bash
dotnet add package Franz.Common.AzureCosmosDB
```

## EF Core Cosmos context

```csharp
using Franz.Common.AzureCosmosDB.Extensions;

builder.Services.AddFranzCosmosDbContext<ApplicationCosmosDbContext>(
    builder.Configuration);
```

The extension binds the `Cosmos` configuration section to `CosmosOptions`, validates required endpoint, key, and database settings on startup by default, and configures EF Core's Cosmos provider. Setting `Enabled` to `false` skips provider configuration but does not bypass those required-option checks when `validateOnStart` is enabled; the native tenant repository also throws if its client is resolved while Cosmos is disabled.

For the broader EF repository/factory composition:

```csharp
builder.Services.AddCosmosInfrastructure<ApplicationCosmosDbContext>(
    builder.Configuration);
```

This composes the Cosmos context, entity repositories, GUID v7 ID generator, and entity factory registration. Entity-repository discovery inspects DbSet properties at runtime and uses reflection to close repository types; this helper is not a Native AOT registration path.

## Native tenant-partitioned documents

For native Cosmos SDK documents, implement `ICosmosTenantDocument` and register the repository with the container name:

```csharp
builder.Services.AddCosmosTenantRepository<CustomerDocument>("customers");
```

The target Cosmos container must use `/tenantId` as its partition-key path. The helper registers the concrete `CosmosTenantRepository<TDocument>`; resolve that concrete type or add an explicit interface mapping. The repository obtains the validated `ITenantExecutionContextAccessor` from the current scope and scopes operations to that tenant. This package does not create the tenant execution context; register it through the HTTP, messaging, or application-owned multi-tenancy integration used by the host. Documents use Cosmos-compatible lower-camel JSON names, including `id` and `tenantId`.

This native repository is distinct from the EF Core `IEntityRepository<TEntity, TKey>` registration.

## Cosmos messaging stores

```csharp
builder.Services.AddFranzCosmosMessaging(builder.Configuration);
```

This registers `CosmosMessagingDbContext` and the Cosmos EF implementations of `IMessageStore` and `IInboxStore`. The package owns Cosmos-specific store mapping; transport hosting remains in the relevant messaging-hosting package.

## Configuration

```json
{
  "Cosmos": {
    "Enabled": true,
    "AccountEndpoint": "https://<account>.documents.azure.com:443/",
    "AccountKey": "<secret>",
    "DatabaseName": "application",
    "ApplicationName": "orders-api"
  }
}
```

Keep account keys in a secret manager. Set `Enabled` deliberately and validate endpoint, database, throughput, partition-key, and indexing choices against the actual deployment. The current EF and native-client setup disables TLS certificate validation when `ApplicationName` contains `Tests`; reserve that marker for isolated test configuration and never use it in production.

## Tenant isolation

The EF Cosmos integration composes the framework tenant filter when multi-tenancy is enabled and the context receives a validated tenant execution accessor. Native tenant repositories use the tenant ID as the Cosmos partition key. These are distinct persistence paths; configure the matching entity/document contracts and container partition key for each.

## License

MIT. See the repository [LICENSE](../../LICENSE).
