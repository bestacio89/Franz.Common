# Franz.Common.Http.MultiTenancy

> Tenant resolution, membership authorization, and immutable tenant execution context for ASP.NET Core requests.

## Overview

The HTTP integration resolves a tenant from application-registered strategies, validates its status, optionally validates membership for authenticated principals, resolves a verified domain, and establishes a request-scoped `TenantExecutionContext`.

The package does not treat a caller-supplied tenant identifier as authorization proof. Applications own the tenant store, resolver strategies, and membership policy.

## Installation

```bash
dotnet add package Franz.Common.Http.MultiTenancy
```

## Registration

```csharp
using Franz.Common.Http.MultiTenancy.Extensions;

builder.Services.AddFranzMultiTenancy(options =>
{
    options.RequireTenant = true;
    options.RequireAuthenticatedAuthorization = true;
    options.ExcludedPaths.Add("/health");
});

// Register the application's ITenantStore and one or more ITenantResolver
// implementations using the application's persistence and resolution rules.
// Register ITenantRequestAuthorizer for authenticated tenant membership.
```

`AddFranzMultiTenancy` registers the HTTP context accessors, tenant execution-context accessor, default resolution pipelines, middleware, options validation, and startup validation. It does not invent a tenant store or a resolver strategy. When tenant resolution is required, startup validation expects at least one `ITenantResolver`.

For authenticated requests, the default is fail-closed: an `ITenantRequestAuthorizer` must be registered. Applications with domain-specific membership, subscription, role, or delegated-access rules should implement this interface.

The package also provides an explicit GUID-claim membership authorizer:

```csharp
builder.Services.AddTenantClaimsAuthorizer(options =>
{
    options.TenantIdClaimType = "tenant_id";
});
```

This authorizer accepts only an authenticated principal whose configured claim parses to the exact resolved tenant GUID. It is a simple membership policy, not a substitute for richer authorization rules.

## Middleware

Use `UseFranzMultiTenancy()` when composing the pipeline manually. With `Franz.Common.Http.Bootstrap`, `UseHttpArchitecture()` already inserts tenant resolution after authentication and before endpoint authorization; do not register the middleware twice.

The validated context is immutable for the duration of the request and is cleared in a `finally` block. Attempts to replace tenant or domain identity while the context is active are rejected. Request cancellation is propagated through resolution, validation, and authorization.

## Resolution and security behavior

- Tenant resolution strategies are application-registered `ITenantResolver` implementations.
- A missing or unresolved tenant is rejected when `RequireTenant` is enabled.
- Inactive tenants and failed tenant validation are rejected.
- Authenticated requests require successful tenant membership authorization when `RequireAuthenticatedAuthorization` is enabled.
- Domain context is established only for a verified domain associated with the resolved tenant.
- Excluded paths are explicit path prefixes matched on segment boundaries, case-insensitively. Excluded endpoints bypass tenant resolution and must be secured independently.
- The root path cannot be excluded.

## Accessing the validated context

Use the provider-neutral contracts from `Franz.Common.MultiTenancy`:

- `ITenantExecutionContextAccessor.Current` for the immutable execution context.
- `ITenantContextAccessor.GetCurrentTenantId()` for the resolved tenant identifier.
- `IDomainContextAccessor.GetCurrentDomainId()` for the resolved domain identifier.

Do not read raw headers directly to establish trusted tenant identity.

## License

MIT. See the repository [LICENSE](../../LICENSE).
