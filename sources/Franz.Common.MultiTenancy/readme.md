# Franz.Common.MultiTenancy

> Provider-neutral tenant and domain identity, resolution, validation, and execution-context contracts.

## Overview

This package defines the tenant boundary shared by HTTP, messaging, persistence, and other infrastructure adapters. It does not resolve an HTTP request or message by itself, and it does not provide a production tenant database.

The important distinction is between **resolution** and **validated execution**:

1. Resolvers identify candidate tenant/domain identities from an adapter-specific context.
2. The resolution pipeline evaluates configured strategies and rejects contradictory successful tenant identities.
3. The adapter loads and validates the tenant, applies its authorization policy, and only then initializes a scoped TenantExecutionContext.
4. The execution context is cleared when the request or message delivery ends.

A resolved identifier is not, by itself, proof that a caller or message producer is authorized for that tenant.

## Installation

~~~bash
dotnet add package Franz.Common.MultiTenancy
~~~

## Core contracts

- **ITenantResolver** and **IDomainResolver** represent resolution strategies. Resolver ordering is controlled by Order.
- **ITenantResolutionPipeline** and **IDomainResolutionPipeline** compose those strategies.
- **ITenantStore** provides tenant lookup by ID or verified host, enumeration, and existence checks.
- **ITenantValidator.IsValidAsync(Guid, CancellationToken)** provides application-specific tenant lifecycle validation.
- **ITenantExecutionContextAccessor** exposes the validated execution context and supports initialization and clearing.
- **ITenantContextAccessor** and **IDomainContextAccessor** expose the current tenant and domain identities.
- **ITenantEntity** and **IGlobalEntity** classify tenant-owned and global domain entities.

The core package intentionally does not prescribe how a tenant is identified from an HTTP request, broker message, or other transport. Those adapters provide the relevant resolvers and lifecycle integration.

## Resolution behavior

DefaultTenantResolutionPipeline orders all registered resolvers by ascending Order, evaluates them, and rejects the result when successful strategies identify different tenant IDs. A failed or empty strategy does not override a successful candidate.

The cancellation-aware resolver and store overloads should be implemented by I/O-backed adapters so cancellation reaches the underlying provider. The default interface implementations preserve compatibility with implementations of the original overloads, but can only observe cancellation before delegating.

## Tenant execution context

A TenantExecutionContext represents an already validated tenant and optional domain for one execution scope. It is not a process-wide or singleton context.

Use a scoped implementation of ITenantExecutionContextAccessor. HTTP requests and message deliveries must not share mutable tenant state. Initialize the context only after tenant lifecycle checks and authorization succeed, and clear it in a finally/disposal path.

Tenant status and verified domain ownership are part of the boundary. A hostname mapping must be globally unique among verified tenant domains. InMemoryTenantStore is intended for tests and local demonstrations; it is not a production tenant registry.

## Adapter packages

| Concern | Package |
| --- | --- |
| Core tenant contracts and execution context | Franz.Common.MultiTenancy |
| HTTP resolution, middleware, and request authorization | Franz.Common.Http.MultiTenancy |
| Message metadata, delivery context, and producer authorization | Franz.Common.Messaging.MultiTenancy |
| EF Core tenant query/write enforcement and database routing | Franz.Common.EntityFramework |

The core package has no service-registration extension. Register its contracts and scoped context through the adapter that owns the execution boundary.

## Important configuration distinction

Tenant isolation activation and database routing are separate decisions. EF Core's MultiTenancyOptions.Enabled controls tenant query/write enforcement. DatabaseIsolationMode independently selects SharedDatabase, TenantDatabase, or the explicit DomainDatabase routing mode. Tenant database routing requires a validated tenant context and never falls back to domain identity.

## License

MIT. See the repository [LICENSE](../../LICENSE).
