# Franz.Common.Identity

> Provider-neutral identity contracts and models for authenticated application execution.

## Overview

This package defines the core identity model independently of ASP.NET Core, HTTP, or any specific identity provider. Authentication and HTTP claim extraction are implemented by integration packages.

## Installation

```bash
dotnet add package Franz.Common.Identity
```

## Identity model

`FranzIdentityContext` carries the current identity values:

- `UserId`: optional GUID subject identifier.
- `Email`: optional email address.
- `FullName`: optional display name.
- `Roles`: optional role-name array.
- `TenantId`: optional validated tenant identifier.
- `DomainId`: optional validated domain identifier.

`IIdentityContextAccessor` exposes the individual values through `GetCurrentId()`, `GetCurrentEmail()`, `GetCurrentFullName()`, `GetCurrentRoles()`, `GetCurrentTenantId()`, and `GetCurrentDomainId()`, as well as the nullable aggregate `GetCurrentIdentity()`.

## Framework integration

The core package does not register an ASP.NET Core implementation or an authentication provider.

- `Franz.Common.Http.Identity` provides `AddHttpIdentityContext()` and the HTTP-backed accessor.
- `Franz.Common.Http.Identity` also contains provider registration extensions for OIDC, SAML2, WS-Federation, and Keycloak.
- `Franz.Common.Http.Authentication` provides the explicit symmetric JWT bearer configuration helper.

Tenant and domain values should come from a validated tenant execution context, not from arbitrary request headers or untrusted claims.

## Testing

Use a test implementation of `IIdentityContextAccessor` in unit tests. Do not make production services depend on `HttpContext` or a provider-specific claims principal when the provider-neutral contract is sufficient.

## License

MIT. See the repository [LICENSE](../../LICENSE).
