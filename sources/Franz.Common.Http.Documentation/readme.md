# **Franz.Common.Http.Documentation**

A robust library within the **Franz Framework** designed to simplify and enhance API documentation for **ASP.NET Core** applications. This package provides seamless integration with native .NET OpenAPI, API versioning, Scalar UI, and route conventions — enabling clear, versioned, and comprehensive API documentation with zero third-party documentation dependencies.

---

## **Features**

- **Native OpenAPI Integration** *(since 2.2.13)*:
  - Uses `Microsoft.AspNetCore.OpenApi` — the official .NET 10 OpenAPI pipeline.
  - No Swashbuckle dependency — eliminates `Microsoft.OpenApi` version conflict hell permanently.
  - One OpenAPI document per API version, served at `/openapi/{version}/openapi.json`.

- **Scalar UI** *(since 2.2.13)*:
  - Replaces Swagger UI with `Scalar.AspNetCore` — cleaner interface, better auth support, multi-version switcher.
  - Available at `/scalar/{documentName}` (e.g. `/scalar/v1`).

- **API Versioning**:
  - Built-in support via `Asp.Versioning.Mvc.ApiExplorer` — the officially maintained successor to the legacy `Microsoft.AspNetCore.Mvc.Versioning` package.
  - Fluent `.AddApiExplorer()` chained on `AddApiVersioning()` replaces the old `AddVersionedApiExplorer()` pattern.

- **Route Customization**:
  - `RoutePrefixConvention` for consistent routing across controllers.
  - Default route prefix: `api/v{version:apiVersion}`.

- **EnumerationClass Schema Support**:
  - `OpenApiSchemaExtensions.ConvertEnumeration()` maps `EnumerationClass` types from Contracts assemblies to correct OpenAPI scalar types via document transformers.

- **Dependency Injection**:
  - `FranzDocumentationBuilder` fluent builder for composing documentation pipeline.
  - `AddFranzDocumentation()` → `ConfigureApiVersioning()` → `ConfigureOpenApi()`.

---

## **Version Information**

**Current Version:** v2.2.17
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

| Package | Version |
|---|---|
| `Asp.Versioning.Mvc.ApiExplorer` | 10.0.0 |
| `Microsoft.AspNetCore.OpenApi` | 10.0.9 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.9 |
| `Scalar.AspNetCore` | 2.16.6 |
| `Franz.Common.Business` | 2.2.14 |
| `Franz.Common.Reflection` | 2.2.14 |

> **Removed since v2.2.14:**
> `Swashbuckle.AspNetCore.SwaggerGen`, `Swashbuckle.AspNetCore.SwaggerUI`,
> `Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer`, `Microsoft.OpenApi` (explicit pin no longer needed).

---

## **Installation**

### **From Private Azure Feed**

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

Install the package:

```bash
dotnet add package Franz.Common.Http.Documentation
```

---

## **Usage**

### **1. Register Documentation Pipeline**

Called automatically by `AddHttpArchitecture`. For manual registration:

```csharp
services
    .AddFranzDocumentation()
    .ConfigureApiVersioning()
    .ConfigureOpenApi();
```

### **2. API Versioning**

Every controller must declare `[ApiVersion]` for version discovery:

```csharp
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/heroes")]
public sealed class HeroController : ControllerBase { }
```

Default versioning behaviour:
- Default version: `1.0`
- Assumes default version when unspecified: `true`
- Readers: URL segment, `x-api-version` header, `x-api-version` media type

### **3. OpenAPI Documents**

One document registered per discovered API version:

```
GET /openapi/v1/openapi.json   → OpenAPI spec for v1
GET /openapi/v2/openapi.json   → OpenAPI spec for v2
```

### **4. Scalar UI**

```
GET /scalar/v1   → Scalar API reference for v1
GET /scalar/v2   → Scalar API reference for v2
```

### **5. Route Prefix Customization**

```csharp
using Franz.Common.Http.Documentation.Routing;

services.AddMvc(options =>
{
    options.Conventions.Add(new RoutePrefixConvention("api/v{version:apiVersion}"));
});
```

### **6. EnumerationClass Schema Mapping**

Applied automatically via `ConfigureOpenApi()`. To apply manually:

```csharp
services.AddOpenApi(options =>
{
    options.ConvertEnumeration();
});
```

---

## **Middleware**

`UseDocumentation()` is called automatically by `UseHttpArchitecture()`.
For manual registration:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDocumentation();
}
```

This registers:
- `MapOpenApi()` per version → `/openapi/{groupName}/openapi.json`
- `MapScalarApiReference()` → `/scalar/{documentName}`

---

## **Migration from v2.2.14 → v2.2.14**

| Before (Swashbuckle) | After (Native OpenAPI) |
|---|---|
| `ConfigureSwagger()` | `ConfigureOpenApi()` |
| `ConfigureSwaggerOptions` | `ConfigureVersionedOpenApiOptions` |
| `SwaggerGenOptionsExtensions.ConvertEnumeration` | `OpenApiSchemaExtensions.ConvertEnumeration` |
| `UseSwagger()` + `UseSwaggerUI()` | `MapOpenApi()` + `MapScalarApiReference()` |
| `/swagger/index.html` | `/scalar/v1` |
| `/swagger/v1/swagger.json` | `/openapi/v1/openapi.json` |
| `AddVersionedApiExplorer()` | `.AddApiExplorer()` chained on `AddApiVersioning()` |
| `Microsoft.AspNetCore.Mvc.Versioning` | `Asp.Versioning.Mvc` |

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Documentation** package integrates seamlessly with:
- **Franz.Common.Http.Bootstrap** — `AddHttpArchitecture` and `UseHttpArchitecture` wire it automatically.
- **Franz.Common.Business** — provides `EnumerationClass` utilities consumed by schema mapping.
- **Franz.Common.Reflection** — reflection utilities for dynamic configuration.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team.
1. Clone the repository at https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request for review. All PRs must comply with **Franz Tribunal** architecture rules.

---

## **License**

MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### v2.2.13 — Native OpenAPI + Scalar Migration

**Breaking changes:**

- `ConfigureSwagger()` renamed to `ConfigureOpenApi()` — update all call sites.
- `ConfigureSwaggerOptions` removed — replaced by `ConfigureVersionedOpenApiOptions`.
- `SwaggerGenOptionsExtensions` removed — replaced by `OpenApiSchemaExtensions`.
- `UseSwagger()` / `UseSwaggerUI()` removed from `UseDocumentation()`.
- Swagger UI at `/swagger/index.html` replaced by Scalar UI at `/scalar/v1`.
- OpenAPI spec moved from `/swagger/v1/swagger.json` to `/openapi/v1/openapi.json`.

**Added:**

- `Microsoft.AspNetCore.OpenApi` native pipeline — one document per API version.
- `Scalar.AspNetCore` — replaces Swagger UI entirely.
- `ConfigureVersionedOpenApiOptions` — `IConfigureOptions<OpenApiOptions>` document transformer
  replacing `IConfigureNamedOptions<SwaggerGenOptions>`.
- `OpenApiSchemaExtensions.ConvertEnumeration()` — document transformer replacing
  `SwaggerGenOptions.MapType()` overrides.
- `OpenApiSchema.Type` now uses `JsonSchemaType` enum (aligned with `Microsoft.OpenApi 2.x`).

**Removed:**

- `Swashbuckle.AspNetCore.SwaggerGen`
- `Swashbuckle.AspNetCore.SwaggerUI`
- `Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer` (legacy, unmaintained)
- `Microsoft.OpenApi` explicit version pin (no longer needed — SDK manages it)

**Root cause resolved:**
`Microsoft.OpenApi 3.x` breaks `IOpenApiRequestBody.Content` which Swashbuckle 10.x
depends on, causing `MissingMethodException` at `/swagger/v1/swagger.json` generation.
Native OpenAPI eliminates this entire class of version conflict permanently.

---

### v2.0.1 — Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

### v1.3 — .NET 9 Upgrade

- Upgraded to **.NET 9.0.8**.
- Separated business concepts from mediator concepts.
- Now compatible with both the in-house Franz mediator and MediatR.

---

### v1.2.65 — Initial .NET 9 Migration

- Upgraded to **.NET 9**.