# Franz.Common.Http

> ASP.NET Core MVC registration, CORS, error strategies, JSON configuration, routing conventions, and forwarded-header setup.

## Overview

This package provides composable ASP.NET Core HTTP primitives. The higher-level AddHttpArchitecture and UseHttpArchitecture composition belongs to Franz.Common.Http.Bootstrap.

## Installation

~~~bash
dotnet add package Franz.Common.Http
~~~

## Service composition

The package uses a fluent FranzHttpBuilder. Start with AddFranzHttp, then configure the required capabilities:

~~~csharp
using Franz.Common.Http.Extensions;

services
    .AddFranzHttp()
    .AddDefaultCors(configuration)
    .AddErrors()
    .AddSerialization()
    .AddFrenchRouting()
    .AddForwardedHeaders(options =>
    {
        // Add the deployment's trusted KnownProxies/KnownNetworks here.
    });
~~~

AddFranzHttp registers MVC controllers and returns the builder used by the remaining service extensions.

## CORS

AddDefaultCors reads the semicolon-separated Cors configuration value. Each value must be an exact HTTP or HTTPS origin with no wildcard, path, query, fragment, or embedded credentials.

The policy allows any header and method. Credentials are allowed only when at least one valid origin is configured. With no configured origins, the policy does not grant cross-origin access.

## Error handling

AddErrors registers ErrorResponseProvider, the built-in IErrorResponseStrategy implementations, and ExceptionFilter. It also adds the filter to MVC options.

The response provider selects the applicable strategy for the exception. Production technical errors should use the sanitized default strategy response rather than exposing raw exception details.

Do not manually register the old ErrorResponseProvider/ExceptionFilter pattern shown in earlier package examples; use AddErrors() so the strategy set and MVC filter are composed consistently.

## Serialization

AddSerialization registers FranzJson.Default and configures MVC JsonOptions to use camel-case property naming, case-insensitive property reads, and the Franz JSON converters.

## Routing and localization

AddFrenchRouting adds FrenchControllerParameterTransformer through RouteTokenTransformerConvention.

UseFrenchLocalization configures French as the default and only supported request culture. UseDefaultCors applies the configured default CORS policy.

## Forwarded headers

AddForwardedHeaders enables X-Forwarded-For, X-Forwarded-Proto, and X-Forwarded-Host processing. The default preserves ASP.NET Core's trusted-proxy/network restrictions. Add deployment proxy addresses explicitly through ForwardedHeadersOptions.KnownProxies or KnownNetworks; do not trust arbitrary forwarded headers from public clients.

## HttpContext helpers

HttpContextExtensions provides:

- TryGetValue(headerName, claimType) — checks the requested claim first, then the named request header;
- TryGetValues(headerName, claimType) — returns multiple claim values when present, otherwise the request header values.

These helpers are convenience accessors, not an authentication or authorization boundary. In particular, a header returned by TryGetValue is caller-controlled unless the hosting environment has independently authenticated and validated it. Use the authenticated identity and validated tenant-context packages for security decisions.

## Related package

- Franz.Common.Http.Bootstrap — higher-level service and middleware composition, including authentication, tenant resolution, documentation, health checks, and global exception handling.

## License

MIT. See the repository [LICENSE](../../LICENSE).
