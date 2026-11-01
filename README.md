![Franz Logo](https://github.com/bestacio89/Franz.Common/raw/main/Docs/assets/FranzLogo.png)

# Franz.Common

**Deterministic architecture and infrastructure foundations for enterprise .NET systems.**

[![.NET](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%7C%20DDD%20%7C%20CQRS-brightgreen)](https://github.com/bestacio89/Franz.Common)
[![Messaging](https://img.shields.io/badge/Messaging-Kafka%20%7C%20RabbitMQ%20%7C%20Azure-blue)](https://github.com/bestacio89/Franz.Common)
[![Multi-tenancy](https://img.shields.io/badge/Multi--tenancy-Validated%20Execution-9cf)](https://github.com/bestacio89/Franz.Common)
[![License](https://img.shields.io/badge/License-MIT-lightgrey)](LICENSE.MD)

## Overview

Franz.Common is the shared foundation of a modular .NET 10 framework for building maintainable, observable and resilient enterprise applications and event-driven services. It provides explicit building blocks for DDD, CQRS, persistence, messaging, multi-tenancy, identity, resilience, diagnostics and application composition.

Franz favours correctness-first engineering: clear ownership, explicit execution boundaries, fail-closed validation, deterministic dependency injection and provider-specific integration tests. Packages are independently consumable; applications opt into the integrations they need.

## Core capabilities

- **DDD and application foundations:** entities, aggregate factories, domain events, validation and application pipelines.
- **Mediator and mapping:** framework mediator, composable behaviours and mapping/source-generation options.
- **Persistence:** EF Core repositories, unit-of-work orchestration, auditing, soft deletion, tenant-aware filters and relational provider adapters.
- **Messaging:** message contracts, Kafka, RabbitMQ, Azure Service Bus, Azure Event Hubs, MassTransit integration and Outbox/Inbox persistence.
- **Multi-tenancy:** tenant/domain resolution, lifecycle validation, authorization boundaries and scoped execution context across HTTP and message processing.
- **Resilience and observability:** structured logging, telemetry and resilience integrations.
- **HTTP and identity:** ASP.NET Core composition, authentication, identity and service-client integrations.

## Multi-tenancy: validated execution boundaries

Tenant identity is represented as validated execution context. Resolving an identifier is not authorization: HTTP and messaging adapters must establish that the tenant exists, is active, and that the caller or producer is authorized.

The framework exercises tenant isolation across three boundaries:

1. **HTTP:** request middleware resolves and validates tenant identity, authorizes authenticated membership through an application-provided authorizer, stores context in the request scope and clears it after execution.
2. **EF Core persistence:** tenant-owned entities use global query filters, tenant stamping and ownership checks. Tenant identity cannot be reassigned through the framework repository bulk-update API.
3. **Messaging and Outbox:** consumer execution uses per-delivery dependency-injection scopes, validates message tenant/domain metadata before dispatch, and disposes execution context after success or failure.

The shared relational tenant contract is exercised against PostgreSQL 16, MariaDB 11.4, MySQL 8.4, SQL Server 2022 and Oracle Free 23. It covers tenant-scoped reads, missing-context behaviour, cross-tenant writes and supported EF Core set-based/repository bulk operations.

**Security boundary:** EF Core query filters are not a universal database security mechanism. Raw SQL, stored procedures, IgnoreQueryFilters, provider-native bulk APIs and external database clients require trusted tenant predicates or independent database enforcement (for example, row-level security). Cosmos DB and MongoDB require separate provider-specific isolation contracts. Applications remain responsible for trusted identity/producer authorization, tenant lifecycle operations, database permissions and deployment controls.

See [tenant isolation boundaries](Docs/tenant-isolation-boundary-audit.md), [relational provider contract](Docs/relational-provider-contract.md) and [messaging execution lifecycle](docs/multitenancy-execution-lifecycle.md).

## Package map

| Area | Packages |
|---|---|
| Core and composition | Franz.Common, Annotations, DependencyInjection, Hosting |
| Domain and application | Business, Mediator, Mapping |
| Persistence | EntityFramework and relational provider packages, MongoDB, AzureCosmosDB |
| Multi-tenancy | MultiTenancy, Http.MultiTenancy, Messaging.MultiTenancy |
| Messaging | Messaging, Hosting, Kafka, RabbitMQ, Azure Event Bus, Azure Event Hubs, MassTransit and persistence adapters |
| HTTP and identity | HTTP bootstrap, headers, authentication, identity, Refit, SSO and documentation |
| Cross-cutting | Caching, errors, logging, serialization, testing, gRPC and OpenTelemetry |

Each package includes a package-specific readme.md, which is packed into its NuGet artifact.

## Getting started

All framework packages in the 2.3.4 release line target .NET 10. Install only the packages required by your application.

```bash
dotnet add package Franz.Common --version 2.3.4
dotnet add package Franz.Common.Mediator --version 2.3.4
dotnet add package Franz.Common.EntityFramework --version 2.3.4
```

For Kafka hosting and tenant-aware message processing:

```bash
dotnet add package Franz.Common.Messaging.Hosting.Kafka --version 2.3.4
dotnet add package Franz.Common.Messaging.MultiTenancy --version 2.3.4
```

For messaging multi-tenancy, register AddMessagingMultitenancyContext(), an ITenantStore and the application-specific ITenantMessageAuthorizer. For HTTP multi-tenancy, register the HTTP integration after authentication and before authorization/endpoints, and provide the application-specific ITenantRequestAuthorizer.

See [Getting Started](Docs/getting-started.md) and the package READMEs for complete setup.

## Messaging execution model

Generic messaging, Kafka and RabbitMQ hosted paths create a DI scope per delivery, establish message context, initialize tenant execution when registered, execute the selected strategy and clear ambient context in a finally path. Azure Service Bus and Azure Event Hubs use per-delivery scopes when the scope factory is supplied. The Outbox listener creates a fresh scope per stored message and resolves its stores, serializer and dispatcher from that scope.

The generic MassTransit Kafka adapter provides a scoped-handler overload receiving the delivery IServiceProvider. When tenant-aware processing is enabled, its legacy captured-handler overload fails closed because it cannot guarantee dependencies are resolved from the per-delivery scope.

Custom ITenantMessageContextInitializer implementations must implement the explicit-message BeginAsync(Message, CancellationToken) overload to participate in transport paths that initialize from a message object. Its default interface implementation throws NotSupportedException. OutboxMessageListener now requires IServiceScopeFactory and ILogger<OutboxMessageListener>; manual constructor call sites must be updated.

## Persistence and provider qualification

The relational adapters share a behavioural contract but are not interchangeable merely because they implement common interfaces. CI qualification covers PostgreSQL 16, MariaDB 11.4, MySQL 8.4, SQL Server 2022 and Oracle Free 23. Provider migrations, SQL translation, transactions, concurrency, value conversion and operational configuration remain provider-specific responsibilities.

Franz uses EF Core as its persistence execution path. Raw SQL or other deliberate bypasses require independent tenant enforcement and tests.

## Build and test

```bash
git clone https://github.com/bestacio89/Franz.Common.git
cd Franz.Common
dotnet restore Franz.Common.slnx
dotnet build Franz.Common.slnx --configuration Release --no-restore
dotnet test Franz.Common.slnx --configuration Release --no-build --no-restore
```

Provider and broker integration suites use real engines/brokers through Testcontainers where configured. A test result qualifies only the exact provider version and execution paths exercised.

## v2.3.4 — Multi-tenancy execution and relational qualification

- Added a shared real-engine tenant-isolation contract for PostgreSQL, MariaDB, MySQL, SQL Server and Oracle.
- Verified tenant-scoped reads/writes, missing-context behaviour, cross-tenant ownership rejection and supported bulk-operation protections.
- Hardened Kafka, RabbitMQ, Azure Service Bus, Azure Event Hubs, Outbox and MassTransit Kafka tenant execution boundaries.
- Added per-delivery DI scopes and tenant-context lifecycle handling, including cleanup on failure and cancellation.
- Added tenant/domain metadata propagation and explicit-message initialization.
- Updated package READMEs and central architecture/security documentation.

This repository change targets package version 2.3.4. NuGet publication remains a separate release operation after restore/audit, Release build, full tests and release approval.

## Documentation

- [Architecture overview](Docs/Architecture/system-architecture.md)
- [Messaging flow](Docs/Architecture/messaging-flow.md)
- [Tenant isolation audit](Docs/tenant-isolation-boundary-audit.md)
- [Relational provider contract](Docs/relational-provider-contract.md)
- [Messaging multi-tenancy lifecycle](docs/multitenancy-execution-lifecycle.md)
- [Versioning policy](Docs/governance/versioning-policy.md)
- [Changelog](changelog.md)

## Contributing and license

Changes should preserve architectural boundaries, include focused unit/integration/architecture tests and update affected package documentation. Use feature branches and pull requests; squash-merge only after required CI checks pass.

MIT License. See [LICENSE.MD](LICENSE.MD).
