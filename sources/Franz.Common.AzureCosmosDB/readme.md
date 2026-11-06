# Franz.Common.AzureCosmosDB

> EF Core Cosmos provider integration, native tenant-partitioned repositories, and Cosmos-backed messaging persistence.

## Overview

This package contains three distinct integration surfaces:

1. EF Core Cosmos DbContext registration and shared Franz repository integration.
2. Native Cosmos SDK repositories that enforce tenant partitioning at the document/container boundary.
3. EF Core Cosmos-backed inbox/outbox stores.

These paths have different storage contracts. Do not assume an EF query filter is a substitute for a native Cosmos partition-key requirement.

## Installation

~~~bash
dotnet add package Franz.Common.AzureCosmosDB
~~~

## Configuration

Cosmos options are bound from the Cosmos section:

~~~json
{
  "Cosmos": {
    "Enabled": true,
    "AccountEndpoint": "https://account.documents.azure.com:443/",
    "AccountKey": "from-secret-configuration",
    "DatabaseName": "orders",
    "ApplicationName": "OrdersService"
  }
}
~~~

Keep the account key in a secret store or environment configuration. Startup validation is enabled by default by the registration methods; it can be disabled explicitly for controlled composition scenarios.

## EF Core infrastructure

Register the Cosmos context and Franz entity repositories:

~~~csharp
using Franz.Common.AzureCosmosDB.Extensions;

services.AddCosmosInfrastructure<OrdersCosmosDbContext>(configuration);
~~~

This composes AddFranzCosmosDbContext, AddCosmosEntityRepositories, the GuidV7 ID generator, and the entity factory. AddFranzCosmosDbContext can also be used directly when only the DbContext registration is required.

A Cosmos context derives from CosmosDbContextBase, which in turn derives from Franz's DbContextBase. It inherits soft-delete behavior and Cosmos model conventions. Tenant query filtering is opt-in: the concrete context must call the CosmosDbContextBase constructor overload that receives ITenantExecutionContextAccessor and IOptions<MultiTenancyOptions>, with MultiTenancyOptions.Enabled set to true. AddCosmosInfrastructure does not register the tenant accessor or enable this option for the application. Cosmos uses its native ETag concurrency mechanism rather than relational concurrency tokens.

## Native tenant-partitioned documents

For native Cosmos SDK document access, use AddCosmosTenantRepository<TDocument>(containerName). The document must implement ICosmosTenantDocument, and the target container must be provisioned with /tenantId as its partition-key path.

The native serializer uses System.Text.Json web defaults so document fields follow Cosmos's required lower-camel naming, including id and tenantId. Each repository operation uses the validated ITenantExecutionContextAccessor and the current tenant partition. A missing tenant context fails closed.

The application must register the tenant execution-context accessor through its HTTP, messaging, or other execution-boundary integration before resolving the tenant repository.

## Messaging persistence

Register Cosmos-backed inbox/outbox stores through:

~~~csharp
services.AddFranzCosmosMessaging(configuration);
~~~

This binds Cosmos options, registers CosmosMessagingDbContext, and provides IMessageStore and IInboxStore through EF Core Cosmos persistence.

## Provider boundaries

- EF Core Cosmos repositories use the DbContext model and its configured query filters.
- Native tenant repositories require a container partition key of /tenantId and use the native Cosmos SDK.
- Cosmos messaging stores are not interchangeable with MongoDB stores.
- EF Core bulk update/delete capabilities are provider-dependent; do not assume relational ExecuteUpdate/ExecuteDelete support on Cosmos.

## License

MIT. See the repository [LICENSE](../../LICENSE).
