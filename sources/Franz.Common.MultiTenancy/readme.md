# Franz.Common.MultiTenancy

Core tenant and domain identity contracts for Franz.Common on .NET 10.

## Purpose

Defines tenant lifecycle, tenant/domain models, resolution contracts and immutable execution context used by HTTP, messaging and persistence integrations. It does not decide how an application authenticates users, provisions tenants or authorizes membership; those policies are supplied by the consuming application.

## Install

```bash
dotnet add package Franz.Common.MultiTenancy --version 2.3.4
```

## Core concepts

- TenantInfo: tenant identity and metadata.
- TenantStatus: provisioning, active, suspended, disabled and deprovisioned lifecycle states.
- TenantDomain: independently identified tenant domain with verification state and normalized IDNA hostname.
- ITenantStore: tenant registry lookup contract.
- ITenantResolver and ITenantResolutionPipeline: candidate tenant resolution strategies and orchestration.
- ITenantValidator: application-specific tenant validation hook.
- TenantExecutionContext: immutable validated identity for one execution scope.
- ITenantExecutionContextAccessor: scoped execution boundary used by infrastructure integrations.

## Security and lifecycle

Resolution is not authorization. A resolver can identify a candidate tenant from a host, header or authenticated claim, but the adapter must validate lifecycle and authorize the relevant user or producer before establishing execution context.

The default resolution pipeline evaluates successful strategies and rejects contradictory tenant identities. Tenant domains have their own identifiers; internationalized hostnames are normalized to IDNA ASCII. Tenant stores must enforce globally unique verified hostname mappings. The in-memory store is intended for tests and local demonstrations, not production tenant registry persistence.

Execution accessors must be scoped and isolated between concurrent requests/messages. Do not register mutable tenant accessors as singletons or copy them between execution scopes.

## Integration packages

- Franz.Common.Http.MultiTenancy establishes validated HTTP request context and authenticated membership authorization.
- Franz.Common.Messaging.MultiTenancy establishes validated tenant context from message metadata and propagates validated tenant/domain metadata.
- Franz.Common.EntityFramework applies tenant-aware EF Core filters and write ownership checks when the accessor is forwarded to DbContextBase.

The cancellation-aware ITenantValidator.IsValidAsync overload retains a default implementation delegating to the original contract, preserving source compatibility while allowing newer validators to observe cancellation.

## Version

Framework release line: **2.3.4**. Target framework: net10.0.

See [tenant isolation audit](../../Docs/tenant-isolation-boundary-audit.md) and [messaging lifecycle](../../docs/multitenancy-execution-lifecycle.md).

MIT License.
