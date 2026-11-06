# Franz.Common.Http.Bootstrap

> ASP.NET Core composition for the shared Franz HTTP architecture and request pipeline.

## Overview

This package composes common Franz HTTP services and middleware: controllers, routing, CORS, forwarded headers, serialization, error handling, authentication entry points, identity context, multi-tenancy, API documentation, and health checks.

It does not automatically configure a JWT/OIDC identity provider and does not auto-register Refit clients.

## Installation

~~~bash
dotnet add package Franz.Common.Http.Bootstrap
~~~

## Service registration

~~~csharp
using Franz.Common.Http.Bootstrap.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpArchitecture(
    builder.Environment,
    builder.Configuration,
    typeof(Program).Assembly);
~~~

AddHttpArchitecture composes the service registrations for the common Franz HTTP stack. It also registers the global ProblemDetails exception handler and validation error response strategy.

Authentication provider configuration remains explicit. For symmetric JWT validation, configure the helper with issuer, audience, and a secret-store-backed signing key:

~~~csharp
using Franz.Common.Http.Authentication.Extensions;

builder.Services
    .AddFranzAuthentication()
    .ConfigureJwtBearer(builder.Configuration);
~~~

For OIDC providers with asymmetric keys, use the provider's standard ASP.NET Core authentication configuration. Do not place OIDC secrets or public keys in the symmetric SigningKey setting.

The application must also register tenant resolvers, tenant storage, and an ITenantRequestAuthorizer when authenticated tenant membership is required. AddHttpArchitecture registers the HTTP multi-tenancy integration, but does not invent tenant data or membership policy.

## Request pipeline

~~~csharp
var app = builder.Build();

app.UseHttpArchitecture();
app.Run();
~~~

UseHttpArchitecture composes the middleware in this order:

1. Exception handling.
2. Forwarded headers and certificate forwarding.
3. Localization, routing, and CORS.
4. Authentication.
5. Tenant resolution and membership authorization.
6. Endpoint authorization and controller execution.
7. API documentation and health checks.

The helper maps controller endpoints and applies RequireAuthorization() to them. Do not add a second MapControllers() call when using this composed pipeline. Configure any intentionally anonymous endpoints explicitly and review excluded tenant paths independently.

## CORS and reverse proxies

CORS and forwarded-header behavior are configured by the shared Franz HTTP modules. Trust only known reverse proxies/networks. Do not clear ASP.NET Core's forwarded-header trust restrictions or accept arbitrary forwarded headers from public clients.

## Refit clients

Refit integration is not automatically enabled or discovered by this package. Register typed Refit clients explicitly through Franz.Common.Http.Refit and the Refit packages required by the application.

The bootstrapper no longer reads a Franz:HttpClients:EnableRefit configuration section or reflectively registers interface types.

## Exception handling

The package registers FranzGlobalExceptionHandler and ProblemDetails support. It maps Franz and standard exceptions through the configured error strategies, returning RFC 7807-compatible responses. Development responses may include diagnostic detail; production responses are sanitized.

## Related packages

- Franz.Common.Http.Authentication — explicit JWT bearer helper.
- Franz.Common.Http.Identity — authenticated identity accessor.
- Franz.Common.Http.MultiTenancy — tenant resolution and authorization.
- Franz.Common.Http.Headers — request header context.
- Franz.Common.Http.Refit — opt-in typed HTTP clients.

## License

MIT. See the repository [LICENSE](../../LICENSE).
