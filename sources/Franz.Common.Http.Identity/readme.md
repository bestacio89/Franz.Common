# Franz.Common.Http.Identity

> Authenticated user identity and validated tenant/domain context for ASP.NET Core applications.

## Overview

This package adapts the Franz identity contracts to the current HTTP request. User identity is read only from an authenticated ClaimsPrincipal. Tenant and domain identity are read only from the validated ITenantExecutionContextAccessor.

The accessor does not treat request headers as an identity source.

## Installation

~~~bash
dotnet add package Franz.Common.Http.Identity
~~~

## Registration

~~~csharp
using Franz.Common.Http.Identity.Extensions;

services.AddHttpIdentityContext();
~~~

AddHttpIdentityContext registers IHttpContextAccessor and the scoped IIdentityContextAccessor implementation. It does not configure an authentication provider or establish tenant context.

## Accessing the current identity

~~~csharp
app.MapGet("/whoami", (IIdentityContextAccessor identityAccessor) =>
{
    var identity = identityAccessor.GetCurrentIdentity();

    return Results.Ok(new
    {
        identity.UserId,
        identity.Email,
        identity.FullName,
        identity.Roles,
        identity.TenantId,
        identity.DomainId
    });
});
~~~

The accessor exposes:

- GetCurrentId() — parses the authenticated NameIdentifier or sub claim as a GUID.
- GetCurrentEmail() — reads the authenticated email claim.
- GetCurrentFullName() — reads the authenticated name claim.
- GetCurrentRoles() — reads role claims and normalizes comma-separated values.
- GetCurrentTenantId() and GetCurrentDomainId() — read the validated tenant execution context.
- GetCurrentIdentity() — returns the combined FranzIdentityContext.

If the current principal is not authenticated, user identity properties return null. Tenant/domain values are null unless a validated execution context is available.

## Authentication providers

Authentication is configured separately. This package does not issue tokens or configure JWT/OIDC/SSO schemes.

The provider integrations exposed by this package include:

- AddFranzWsFederationIdentity(configuration)
- AddFranzOidcIdentity(configuration)
- AddFranzSaml2Identity(configuration)
- AddFranzKeycloakIdentity(configuration)

Use the provider-specific options and security requirements for the chosen identity provider. Keep client secrets and signing material in a secret store.

## Tenant boundary

Register Franz.Common.Http.MultiTenancy when the application needs tenant-aware identity. Tenant/domain IDs are obtained from the validated request execution context, not directly from a JWT tenant claim or caller-supplied header.

Use IHeaderContextAccessor separately when raw inbound headers are needed for non-authoritative application metadata. Never use those values as proof of user identity or tenant membership.

## Related packages

- Franz.Common.Identity — provider-neutral identity contracts and models.
- Franz.Common.Http.Authentication — JWT bearer authentication helper.
- Franz.Common.Http.MultiTenancy — tenant resolution and authenticated membership authorization.
- Franz.Common.Http.Bootstrap — composed ASP.NET Core request pipeline.

## License

MIT. See the repository [LICENSE](../../LICENSE).
