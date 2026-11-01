# Authentication and HTTP trust model

Franz.Common does not mandate an identity provider. Host applications own identity-provider selection, credential issuance, and authentication configuration. Supported integration patterns include OAuth 2.0 / OpenID Connect, JWT bearer authentication, and legacy cookie or federation providers.

## Authentication is not tenant authorization

Authentication establishes the caller's identity. Tenant resolution selects a candidate tenant for the request. Neither a tenant identifier in a JWT claim nor one supplied through a host, header, or query string proves that the caller is a member of that tenant.

The HTTP tenant middleware therefore requires an application-registered `ITenantRequestAuthorizer` for authenticated principals. Missing authorizer registration and denied membership both fail closed with HTTP 403. Implement the authorizer against the application's trusted membership/authorization store.

## JWT bearer helper

`Franz.Common.Http.Authentication` provides a symmetric-signature JWT helper:

```csharp
builder.Services
    .AddFranzAuthentication()
    .ConfigureJwtBearer(builder.Configuration);
```

The default configuration section is `JwtSettings` and must provide:

- `Issuer`: required exact issuer.
- `Audience`: required exact API audience.
- `SigningKey`: required symmetric key containing at least 32 UTF-8 bytes.

The helper always enables issuer, audience, lifetime, expiration, signed-token, and signing-key validation. It does not silently disable a validation dimension when configuration is absent. Missing settings or a weak signing key fail during service registration. Clock skew is zero and HTTPS metadata is required.

Store signing keys in a secret manager or environment-backed configuration. Never commit real credentials. This helper is for symmetric signing; applications using Entra ID, Keycloak, or another OIDC provider with asymmetric signing should configure ASP.NET Core JWT bearer authentication with the provider's authority/metadata and audience validation instead of supplying an HMAC key.

## Middleware order

When using `UseHttpArchitecture()`, Franz composes the request pipeline in this order:

1. Exception handling and forwarded/certificate headers.
2. Routing and CORS.
3. Authentication.
4. Tenant resolution, tenant lifecycle validation, and authenticated membership authorization.
5. Endpoint authorization and controller execution.

Do not add a second `UseFranzMultiTenancy()` call when using the composed pipeline. If composing middleware manually, authentication must run before tenant resolution, and tenant resolution must run before endpoint authorization.

## CORS

The `Cors` configuration value accepts a semicolon-separated list of exact HTTP or HTTPS origins, for example:

```text
Cors=https://app.example.com;https://admin.example.com
```

Wildcard origins, wildcard subdomains, paths, credentials in URLs, queries, and fragments are rejected. Credentials are allowed only when at least one exact origin is configured. When no origins are configured, the default policy does not grant cross-origin access. CORS is a browser access policy, not an authentication or authorization mechanism.

## Forwarded headers

Franz enables forwarded-for, forwarded-proto, and forwarded-host processing but preserves ASP.NET Core's trusted proxy/network restrictions. The default trust is limited to the framework's loopback entries. Deployments behind a reverse proxy must explicitly add the proxy IPs or networks to `ForwardedHeadersOptions.KnownProxies` / `KnownNetworks`. Never trust forwarded headers from arbitrary internet clients.

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownProxies.Add(IPAddress.Parse("10.0.0.10"));
});
```

Use the actual proxy addresses for the deployment and keep `ForwardLimit` aligned with the known proxy chain.

## Identity and tenant context

- User ID, email, display name, and roles are read only from an authenticated `ClaimsPrincipal`.
- Caller-supplied `X-User-*`, `X-Tenant-Id`, and `X-Domain-Id` headers are not identity proof.
- Tenant and domain identity exposed by `IdentityContextAccessor` comes from the validated tenant execution context, not arbitrary request headers or unvalidated claims.
- The tenant execution context is immutable for the request and is cleared in a `finally` block after downstream execution.
- Authenticated membership is checked before the context is exposed to application middleware and endpoints.

Use `IHeaderContextAccessor` when application code intentionally needs raw request headers. Do not interpret those values as authenticated identity.

## Cancellation

HTTP tenant resolution uses `HttpContext.RequestAborted` and passes it through the resolution pipeline, resolvers, tenant store, validator, and membership authorizer. I/O-backed `ITenantStore` implementations should override the cancellation-aware overloads and pass the token to their database or remote provider.

## Security boundary

CORS, trusted proxy configuration, authentication, tenant membership authorization, and persistence isolation protect different boundaries. None substitutes for the others. See the [tenant isolation boundary audit](../tenant-isolation-boundary-audit.md) for EF Core, raw SQL, provider, background-job, and messaging responsibilities.
