# Franz.Common.SSO

> Configuration binding and claims-normalization composition for Franz identity integrations.

## Overview

Configuration binding and claims-normalization composition for Franz identity integrations.

## Installation

```bash
dotnet add package Franz.Common.SSO
```

## Register the SSO identity composition

```csharp
using Franz.Common.SSO.Extensions;

builder.Services.AddFranzSsoIdentity(builder.Configuration);
```

The extension binds the `FranzIdentity` configuration section to `FranzSsoSettings`, registers `IHttpContextAccessor`, adds the OIDC and Keycloak claims normalizers, registers `CompositeClaimsTransformation`, and installs a startup filter that logs the configured provider flags.

## Configuration model

`FranzSsoSettings` exposes:

- `AllowMultipleInteractiveProviders`
- `WsFederation`
- `Oidc`
- `Saml2`
- `Keycloak`
- `Jwt`
- `InteractiveProvider`

The provider option types are supplied by the HTTP identity provider packages. Configuration binding alone does not register authentication schemes or handlers.

## Important implementation boundary

The current `AddFranzSsoIdentity` implementation does **not** call `AddAuthentication`, register WS-Federation/SAML/OIDC/Keycloak/JWT authentication handlers, or configure challenge schemes. It registers claims-normalization services and startup logging/validation only. Add the actual provider registration through the owning `Franz.Common.Http.Identity` integration and verify the resulting authentication schemes in the host.

The startup filter registered by the current method is a private nested implementation. Its `enabledInteractive` list is not populated before the multiple-provider check, so the check cannot currently detect multiple enabled interactive providers. Do not rely on `AllowMultipleInteractiveProviders` as an enforced guard until that implementation is corrected.

## Claims normalization

The registered transformation composes the OIDC and Keycloak normalizers. It is applied by ASP.NET Core's claims-transformation pipeline when that pipeline is active. The package does not establish the source identity or prove that incoming claims are trustworthy; authentication must be performed by a correctly configured provider.

## Security and deployment

- Store client secrets and signing material outside source control.
- Configure issuer, audience, metadata, callback, and token validation settings in the actual provider registration.
- Test authentication and claim normalization independently for every enabled provider.
- Do not expose normalized identity endpoints without authorization.

## License

MIT. See [LICENSE](../../LICENSE).
