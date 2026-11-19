# Franz.Common.Http.Documentation

> ASP.NET Core URL-segment API versioning and native OpenAPI/Scalar endpoint registration.

## Overview

This package configures API versioning and native OpenAPI/Scalar endpoint registration for Franz HTTP services.

## Installation

```bash
dotnet add package Franz.Common.Http.Documentation
```

## Service registration

The fluent builder is defined in `Franz.Common.Http.Documentation.Extensions`.

```csharp
using Franz.Common.Http.Documentation.Extensions;

builder.Services
    .AddFranzDocumentation()
    .ConfigureApiVersioning()
    .ConfigureOpenApi();
```

Call `ConfigureApiVersioning()` before `ConfigureOpenApi()`. The standard version reader is exclusively URL-segment based. The default API version is 1.0, the default version is assumed when unspecified, and supported API versions are reported.

The standard route prefix is:

```text
/api/v{version:apiVersion}
```

### Client request contract

The API version belongs in the URL. Clients must not use `MediaTypeApiVersionWriter`, add an `x-api-version` request header, or encode the version as an `Accept` media-type parameter.

Send a normal representation preference:

```http
GET /api/v1/orders
Accept: application/json
```

Do not send:

```http
Accept: application/json; x-api-version=1.0
x-api-version: 1.0
```

The versioning configuration does not rewrite or sanitize client headers. Applications that explicitly replace the configured `ApiVersionReader` own that alternative contract and its associated content-negotiation behavior.

## OpenAPI registration

The current `ConfigureOpenApi()` implementation registers the native OpenAPI document named `v1`. It sets the document title from the entry assembly and the document version to `1.0`. Do not assume that it currently discovers and registers one document for every API version: the implementation shown registers only `v1`.

## Endpoint mapping

Call `UseDocumentation()` from the namespace `Franz.Common.Http.Bootstrap.Extensions` after the application is built and routing is configured.

```csharp
using Franz.Common.Http.Bootstrap.Extensions;

app.UseDocumentation();
```

The extension maps the native OpenAPI endpoint at `/openapi/{documentName}.json` and Scalar's API reference. The current Scalar configuration exposes the `v1` document using `/openapi/v1.json`.

Restrict documentation endpoints according to the deployment's exposure policy. The extension itself does not enforce a Development-only environment condition.

## Route conventions

The versioning registration adds the general route prefix through MVC options. Controllers still need API-version metadata for the versioning explorer to group them correctly.

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
}
```

Avoid applying a second route-prefix convention manually unless the application intentionally replaces the package's default.

## Current implementation boundaries

The project uses ASP.NET Core native OpenAPI and Scalar rather than the legacy Swashbuckle integration described in older package history. The current source still contains historical filenames such as `ConfigureSwaggerOptions.cs` and `SwaggerGenOptionsExtensions.cs`; filenames are not a reliable statement of the public API. Verify the active extension signatures in the source before using historical changelog names.

The current document registration is fixed to `v1`; multi-version document discovery and schema-transformer behavior should not be assumed from earlier README text.

## License

MIT. See [LICENSE](../../LICENSE).
