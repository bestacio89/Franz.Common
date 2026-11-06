# Franz.Common.Identity

> Provider-neutral identity context and accessor contracts for user, tenant, and domain identity.

## Overview

This package defines the application-facing identity model. It has no dependency on ASP.NET Core, HttpContext, JWT, OIDC, SAML, WS-Federation, or a specific identity provider.

## Installation

~~~bash
dotnet add package Franz.Common.Identity
~~~

## Identity model

FranzIdentityContext is an immutable-init model with the following properties:

- UserId — nullable GUID identifying the authenticated user.
- Email — nullable email address.
- FullName — nullable display name.
- Roles — nullable role collection.
- TenantId — nullable validated tenant identifier.
- DomainId — nullable validated domain identifier.

## Accessor contract

IIdentityContextAccessor exposes:

~~~csharp
public interface IIdentityContextAccessor
{
    string? GetCurrentEmail();
    Guid? GetCurrentId();
    string? GetCurrentFullName();
    Guid? GetCurrentTenantId();
    Guid? GetCurrentDomainId();
    string[]? GetCurrentRoles();
    FranzIdentityContext? GetCurrentIdentity();
}
~~~

The contract describes the current execution identity. It does not authenticate a caller, validate a tenant, or authorize tenant membership. Those responsibilities belong to the application authentication and tenant-boundary integrations.

The interface is currently declared in the global namespace; the FranzIdentityContext model is in Franz.Common.Identity.

## Testing

FakeIdentityContextAccessor provides an in-memory implementation for unit tests:

~~~csharp
using Franz.Common.Identity;
using Franz.Common.Identity.Testing;

var accessor = new FakeIdentityContextAccessor(new FranzIdentityContext
{
    UserId = Guid.NewGuid(),
    Email = "user@example.test",
    FullName = "Example User",
    Roles = ["reader"],
    TenantId = Guid.NewGuid()
});
~~~

The fake returns the supplied identity values and does not perform authentication or tenant validation.

## ASP.NET Core integration

Use Franz.Common.Http.Identity for the HttpContext-backed accessor and identity-provider integrations. That package owns its own registration extensions and provider-specific configuration. This core package does not contain HTTP identity registration methods.

## Security boundary

Identity values exposed by the accessor must originate from an authenticated principal and validated execution context. Do not populate this model directly from caller-controlled headers or query parameters. TenantId and DomainId are context values, not authorization decisions.

## License

MIT. See the repository [LICENSE](../../LICENSE).
