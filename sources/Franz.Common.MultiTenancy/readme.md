# Franz.Common.MultiTenancy

> Provider-neutral contracts and scoped execution context for tenant and domain isolation.

## Overview

This package defines tenant/domain identity, tenant persistence contracts, validation contracts, and the immutable execution context shared by HTTP, messaging, gRPC, and persistence integrations. It does not choose how a tenant is resolved from an HTTP request or broker message; boundary-specific packages implement those integrations.

## Installation

```bash
dotnet add package Franz.Common.MultiTenancy
```

## Core contracts

- `TenantInfo`: tenant identity, status, and associated domains.
- `ITenantStore`: asynchronous tenant lookup by ID or host, enumeration, and existence checks.
- `ITenantResolver`: boundary-specific tenant resolution strategy.
- `ITenantValidator`: application validation of a resolved tenant.
- `ITenantExecutionContextAccessor`: scoped access to the validated execution context.
- `ITenantContextAccessor` and `IDomainContextAccessor`: tenant/domain identity accessors.
- `ITenantEntity` and `IGlobalEntity`: explicit ownership classifications for persistence models.

## Validated execution context

A `TenantExecutionContext` can be created only for a non-empty, active tenant. It records the tenant, optional verified domain, resolution source, and resolution timestamp. The scoped `ScopedTenantExecutionContextAccessor` allows one initialization per execution scope and exposes an explicit `Clear()` operation.

The context is not a mutable request DTO. Initialize it only after resolution, status validation, and authorization have succeeded. Clear it when the request/message execution scope ends.

## Boundary integrations

Use the appropriate package for each boundary:

- `Franz.Common.Http.MultiTenancy`: HTTP tenant resolution and membership authorization.
- `Franz.Common.Messaging.MultiTenancy`: message-header extraction, producer authorization, and message-scoped execution.
- `Franz.Common.Grpc`: gRPC client/server tenant propagation and resolution.
- `Franz.Common.EntityFramework`: tenant-aware query filters and write enforcement for relational EF contexts.

## Persistence ownership

Tenant-owned entities implement `ITenantEntity` and expose a mapped `Guid TenantId`. Data intentionally shared across tenants implements `IGlobalEntity`. When multi-tenancy is enabled with a shared relational database, the EF integration requires each persisted Franz entity to declare exactly one of these ownership classifications.

Do not treat a tenant ID from an untrusted header or claim as proof of membership. Boundary authorization must validate the caller or transport identity independently.

## License

MIT. See the repository [LICENSE](../../LICENSE).
