# Franz.Common.Http.Identity

> ASP.NET Core identity-context access backed by authenticated claims and validated tenant execution state.

## Overview

This package provides the HTTP implementation of Franz identity access. It does not authenticate users itself. Authentication is configured by the host through ASP.NET Core or one of the provider registration extensions included in this package.

## Installation

```bash
dotnet add package Franz.Common.Http.Identity
```

## Register the identity context

```csharp
using Franz.Common.Http.Identity.Extensions;

builder.Services.AddHttpIdentityContext();
```

Inject `IIdentityContextAccessor` into application services or endpoints:

```csharp
app.MapGet("/whoami", (IIdentityContextAccessor identity) =>
{
    var current = identity.GetCurrentIdentity();

    return Results.Ok(new
    {
        current.UserId,
        current.Email,
        current.FullName,
        current.Roles,
        current.TenantId,
        current.DomainId
    });
});
```

The accessor reads user ID, email, display name, and roles only from an authenticated `ClaimsPrincipal`. Tenant and domain identifiers are read from `ITenantExecutionContextAccessor`, which is established by the validated tenant-resolution pipeline. Unauthenticated principals produce no user identity.

## Authentication provider registration

The package includes optional provider-specific registration methods. Use only the provider required by the application:

| Provider | Registration method | Configuration section |
|---|---|---|
| WS-Federation | `AddFranzWsFederationIdentity(configuration)` | `FranzIdentity:WsFederation` |
| OpenID Connect | `AddFranzOidcIdentity(configuration)` | `FranzIdentity:Oidc` |
| SAML 2.0 | `AddFranzSaml2Identity(configuration)` | `FranzIdentity:Saml2` |
| Keycloak (OIDC) | `AddFranzKeycloakIdentity(configuration)` | `FranzIdentity:Keycloak` |

For example:

```csharp
using Franz.Common.Http.Identity.Providers.OIDC;

builder.Services.AddFranzOidcIdentity(builder.Configuration);
```

Each provider extension configures ASP.NET Core authentication. These provider methods do not replace `AddHttpIdentityContext()`; register the identity accessor separately when the application needs `IIdentityContextAccessor`.

Provider configuration contains credentials and identity-provider metadata. Keep client secrets in a secret manager or protected deployment configuration; never commit real credentials.

## Trust boundary

- User identity is never inferred from arbitrary request headers.
- A tenant claim is not, by itself, proof that the user belongs to the resolved tenant.
- Tenant and domain identity are exposed only from the validated tenant execution context.
- Use `IHeaderContextAccessor` for raw message/request header metadata; do not use it as an authentication source.

## Related packages

- `Franz.Common.Identity`: provider-neutral identity contracts and models.
- `Franz.Common.Http.Authentication`: strict symmetric JWT bearer configuration.
- `Franz.Common.Http.MultiTenancy`: tenant resolution and request authorization.

## License

MIT. See the repository [LICENSE](../../LICENSE).
