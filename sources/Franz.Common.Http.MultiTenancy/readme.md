# Franz.Common.Http.MultiTenancy

> ASP.NET Core tenant resolution, validated request context, and authenticated tenant-membership enforcement.

## Overview

This package integrates the core Franz tenant contracts with the ASP.NET Core request pipeline. It provides HTTP resolvers, tenant/domain accessors backed by server-established request state, middleware, startup validation, and an authorization boundary for authenticated principals.

Tenant resolution identifies a candidate tenant. It does not establish that the authenticated user is a member of that tenant.

## Installation

~~~bash
dotnet add package Franz.Common.Http.MultiTenancy
~~~

## Service registration

~~~csharp
using Franz.Common.Http.MultiTenancy.Extensions;

services.AddFranzMultiTenancy(options =>
{
    options.RequireTenant = true;
    options.ExcludedPaths.Add("/health");
    options.ExcludedPaths.Add("/metrics");
});

// Register the application's ITenantStore and one or more ITenantResolver
// implementations. Register ITenantValidator when lifecycle checks are required.
// Register an ITenantRequestAuthorizer for authenticated tenant membership.
~~~

AddFranzMultiTenancy enables core multi-tenancy, registers the HTTP context accessor, scoped tenant/domain/execution-context accessors, resolution pipelines, middleware, and startup validation.

The package does not silently invent a tenant resolver or tenant store. When RequireTenant is enabled (the default), host startup fails if no ITenantResolver is registered.

## Middleware order

Place tenant resolution after authentication and before authorization and endpoint execution:

~~~csharp
app.UseAuthentication();
app.UseFranzMultiTenancy();
app.UseAuthorization();
~~~

The middleware resolves a candidate, loads and validates the tenant, authorizes authenticated membership, establishes the immutable TenantExecutionContext, and clears request state when downstream execution exits.

## Resolution strategies

The package provides header, host, query-string, and authenticated-claim tenant resolvers. Register only the strategies appropriate to the application and configure their ordering explicitly. If successful strategies identify different tenant IDs, the resolution pipeline rejects the request rather than choosing one arbitrarily.

A caller-controlled header, route value, query parameter, or claim is not by itself authorization. For authenticated requests, register an ITenantRequestAuthorizer that checks membership against the application's trusted identity or membership source.

## Authenticated tenant authorization

Authenticated requests require an ITenantRequestAuthorizer by default. If the service is missing or authorization is denied, the request is rejected. Applications may register their own implementation:

~~~csharp
services.AddScoped<ITenantRequestAuthorizer, ApplicationTenantRequestAuthorizer>();
~~~

For the narrow policy where an authenticated principal must carry a GUID claim exactly matching the resolved tenant ID, opt in to the built-in authorizer:

~~~csharp
services.AddTenantClaimsAuthorizer(options =>
{
    options.TenantIdClaimType = "tenant_id";
});
~~~

The built-in claims authorizer requires an authenticated principal and an exact GUID match. It is not a substitute for richer membership, role, subscription, or delegated-access policies.

Setting RequireAuthenticatedAuthorization to false disables this middleware authorization boundary and must be an explicit, security-reviewed application decision.

## Excluded paths

ExcludedPaths contains absolute path prefixes matched case-insensitively on segment boundaries. The root path cannot be excluded, and invalid paths are rejected during options validation. Excluded endpoints bypass tenant resolution and context creation; secure them independently.

## Context and cancellation

Tenant and domain accessors read server-established values from HttpContext.Items. They do not treat raw inbound headers as authoritative ambient context. The middleware propagates RequestAborted through resolver, tenant-store, validator, and authorizer calls, then clears the execution context in a finally path.

Implementations of ICurrentUserService or other identity integrations must rely on authenticated claims, not caller-supplied identity headers.

## Related packages

- Franz.Common.MultiTenancy — provider-neutral contracts and execution context.
- Franz.Common.Messaging.MultiTenancy — tenant context for message delivery.
- Franz.Common.Http.Authentication — authentication integration.

## License

MIT. See the repository [LICENSE](../../LICENSE).
