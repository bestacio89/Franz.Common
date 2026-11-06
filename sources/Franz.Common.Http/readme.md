# Franz.Common.Http

> ASP.NET Core HTTP primitives for controller registration, CORS, error strategies, serialization, routing, and forwarded headers.

## Overview

This package contains the lower-level HTTP service-registration and middleware extensions used by the Franz HTTP bootstrap package. Error handling is registered through the fluent `AddErrors()` capability and the bootstrap package's global exception handler.

## Installation

```bash
dotnet add package Franz.Common.Http
```

## Registering HTTP services

`AddFranzHttp()` registers MVC controllers and returns a fluent `FranzHttpBuilder`. Additional capabilities are composed through that builder:

```csharp
using Franz.Common.Http.Extensions;

builder.Services
    .AddFranzHttp()
    .AddErrors()
    .AddSerialization()
    .AddFrenchRouting()
    .AddDefaultCors(builder.Configuration)
    .AddForwardedHeaders();
```

The available builder operations are:

- `AddErrors()`: registers the error response provider, built-in response strategies, and MVC exception filter.
- `AddSerialization()`: configures the framework's JSON defaults and converters.
- `AddFrenchRouting()`: installs the French controller-parameter transformer convention.
- `AddDefaultCors(configuration)`: configures exact allowed origins from the `Cors` setting.
- `AddForwardedHeaders()` or `AddForwardedHeaders(configure)`: configures forwarded-header processing while preserving trusted-proxy restrictions.

## Error response strategies

`AddErrors()` registers `IErrorResponseProvider`, `IErrorResponseStrategy` implementations, and `ExceptionFilter`. Applications can add or replace strategy registrations through dependency injection. Technical errors must use sanitized responses in production; do not expose raw exception details to clients.

The higher-level `Franz.Common.Http.Bootstrap` package registers the global `FranzGlobalExceptionHandler` and ProblemDetails support as part of `AddHttpArchitecture(...)`.

## Middleware helpers

The package exposes `UseFrenchLocalization()` and `UseDefaultCors()` as application-builder extensions. The standard composed middleware pipeline is owned by `Franz.Common.Http.Bootstrap` through `UseHttpArchitecture()`.

## CORS and forwarded headers

The `Cors` configuration value is a semicolon-separated list of exact HTTP/HTTPS origins. Wildcards and origin paths are rejected. An empty list does not grant cross-origin access.

Forwarded headers are not trusted from arbitrary clients. Configure the deployment's known proxy addresses or networks using ASP.NET Core's `ForwardedHeadersOptions`.

## License

MIT. See the repository [LICENSE](../../LICENSE).
