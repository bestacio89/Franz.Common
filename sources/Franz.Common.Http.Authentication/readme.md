# Franz.Common.Http.Authentication

JWT bearer authentication helpers for Franz.Common on .NET 10.

## Installation

```bash
dotnet add package Franz.Common.Http.Authentication
```

## Symmetric JWT configuration

The package exposes a fluent helper for applications that issue and validate symmetric-signature JWTs.

```csharp
using Franz.Common.Http.Authentication.Extensions;

builder.Services
    .AddFranzAuthentication()
    .ConfigureJwtBearer(builder.Configuration);
```

The default configuration section is `JwtSettings`:

```json
{
  "JwtSettings": {
    "Issuer": "https://identity.example.com",
    "Audience": "orders-api",
    "SigningKey": "<load-a-32-byte-or-longer-secret-from-a-secret-store>"
  }
}
```

Required settings:

| Setting | Contract |
|---|---|
| `Issuer` | Required; exact issuer validation is always enabled. |
| `Audience` | Required; exact audience validation is always enabled. |
| `SigningKey` | Required symmetric key of at least 32 UTF-8 bytes. |

The helper always requires signed tokens and expiration, validates issuer, audience, lifetime, and signing key, and uses zero clock skew. Missing values or an undersized key fail during service registration. Validation is never silently disabled to accommodate incomplete configuration.

Load signing material from Key Vault, another secret manager, or protected environment configuration. Do not commit real signing keys or place them in ordinary application settings files.

This helper is specifically for symmetric signing. For Entra ID, Keycloak, or another OpenID Connect provider using asymmetric keys and discovery metadata, configure the standard ASP.NET Core `AddJwtBearer` options with the provider authority and API audience. Do not place an asymmetric public key or an OIDC client secret in `SigningKey`.

## Integration with HTTP bootstrap

`AddHttpArchitecture(...)` composes authentication middleware into the request pipeline but does not infer or invent an identity provider configuration. Configure the authentication scheme explicitly in the host application.

`UseHttpArchitecture()` orders authentication before tenant resolution and endpoint authorization. For authenticated principals, tenant resolution requires an application-provided `ITenantRequestAuthorizer`; a tenant claim alone is not membership proof.

## Related documentation

- [Franz authentication and HTTP trust model](../../Docs/security/authentication-model.md)
- [Tenant isolation boundary audit](../../Docs/tenant-isolation-boundary-audit.md)

## License

MIT. See the repository license.
