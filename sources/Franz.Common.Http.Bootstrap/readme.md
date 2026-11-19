# Franz.Common.Http.Bootstrap

> Composition root for the standard Franz ASP.NET Core HTTP services and middleware pipeline.

## Overview

This package provides two explicit entry points: `AddHttpArchitecture(...)` for service registration and `UseHttpArchitecture()` for middleware composition. It centralizes the framework's HTTP defaults without silently configuring an identity provider or tenant membership policy.

## Installation

```bash
dotnet add package Franz.Common.Http.Bootstrap
```

## Service registration

```csharp
using Franz.Common.Http.Bootstrap.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpArchitecture(
    builder.Environment,
    builder.Configuration,
    typeof(Program).Assembly);
```

The registration composes common architecture services, HTTP controllers, exact-origin CORS, forwarded-header support, serialization, error strategies, JWT authentication entry-point, header context, API documentation, HTTP identity context, tenant resolution, serializers, and health checks.

### API versioning contract

The standard HTTP bootstrap uses URL-segment API versioning exclusively. The version is part of the route (for example, `/api/v1/orders`); it is not read from an `x-api-version` request header or an `Accept` media-type parameter.

Clients should send normal content-negotiation headers, such as `Accept: application/json`. Do not register `MediaTypeApiVersionWriter` for clients targeting the standard Franz HTTP API contract. The versioning setup does not sanitize or rewrite caller-supplied headers.

### Authentication is explicit

`AddHttpArchitecture` calls `AddFranzAuthentication()` to expose the authentication builder, but it does not configure a JWT bearer handler or choose an identity provider. Configure the intended scheme explicitly. For symmetric JWTs:

```csharp
builder.Services
    .AddFranzAuthentication()
    .ConfigureJwtBearer(builder.Configuration);
```

For OIDC, SAML2, WS-Federation, or Keycloak, use the corresponding provider registration from `Franz.Common.Http.Identity`. Do not register multiple competing default authentication schemes unless the application explicitly composes them.

### Tenant membership is explicit

The bootstrapper registers `AddFranzMultiTenancy()`, but the application must provide its `ITenantStore`, at least one `ITenantResolver` when tenant resolution is required, and an `ITenantRequestAuthorizer` for authenticated tenant membership. The built-in GUID-claim authorizer is opt-in through `AddTenantClaimsAuthorizer(...)`.

## Middleware pipeline

```csharp
var app = builder.Build();

app.UseHttpArchitecture();
app.Run();
```

`UseHttpArchitecture()` composes exception handling, forwarded headers, certificate forwarding, localization, routing, CORS, authentication, tenant resolution, authorization, controller endpoints, API documentation, and health checks in the required order.

Do not add a second `UseFranzMultiTenancy()` call when using this composed pipeline. The composed endpoint mapping requires authorization for mapped controllers; configure endpoint policies and anonymous endpoints deliberately.

## Configuration boundaries

- `Cors` is a semicolon-separated list of exact HTTP/HTTPS origins. Wildcards, paths, query strings, fragments, and credentials in an origin are rejected. No configured origins means no cross-origin access is granted.
- Forwarded headers preserve ASP.NET Core trusted-proxy/network restrictions. Add the actual reverse proxies explicitly through `ForwardedHeadersOptions`; do not trust arbitrary senders.
- Symmetric JWT configuration requires issuer, audience, and a secret signing key of at least 32 UTF-8 bytes. Keep secrets in a secret manager.
- Tenant resolution exclusions are explicit path prefixes; excluded endpoints must be secured independently.

## Refit

Refit is not automatically discovered or registered by `AddHttpArchitecture`. Add the `Franz.Common.Http.Refit` integration and register typed clients explicitly using its API.

## Related packages

`Franz.Common.Http`, `Franz.Common.Http.Authentication`, `Franz.Common.Http.Identity`, `Franz.Common.Http.MultiTenancy`, and `Franz.Common.Http.Documentation`.

## License

MIT. See the repository [LICENSE](../../LICENSE).
