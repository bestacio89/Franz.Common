# Franz.Common.Http.Documentation

> ASP.NET Core API-versioning and native OpenAPI/Scalar endpoint registration.

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

Call `ConfigureApiVersioning()` before `ConfigureOpenApi()`. The versioning setup registers controllers with the route prefix `api/v{version:apiVersion}`, sets the default API version to 1.0, assumes the default when the request omits a version, reports supported versions, and combines URL-segment, `x-api-version` header, and `x-api-version` media-type readers.

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
[Route("api/v{version:apiVersion}/orders")]
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
