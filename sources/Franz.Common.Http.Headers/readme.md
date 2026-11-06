# Franz.Common.Http.Headers

> ASP.NET Core adapters for the transport-agnostic header context contract and required-header action constraints.

## Installation

```bash
dotnet add package Franz.Common.Http.Headers
```

## Register HTTP header context access

```csharp
using Franz.Common.Http.Headers.Extensions;

builder.Services.AddHttpHeaderContext();
```

This registers ASP.NET Core's `IHttpContextAccessor` and a scoped `IHeaderContextAccessor` implementation (`HeaderContextAccessor`) using the no-duplicate registration strategy.

The accessor adapts the current request's headers to the `IDictionary<string, string[]>` contract defined by `Franz.Common.Headers`. `ListAll()` returns an empty case-insensitive dictionary when there is no current HTTP context. It filters blank values when building the full dictionary. The raw `TryGetValue` overload returns the values for the requested key. The generic overload returns the first value as-is for `string`; for other types it delegates deserialization to `IJsonSerializer`.

## Require a header for an action

`HeaderRequiredAttribute` can be applied to a controller or action, allows multiple declarations, and takes a CLR type and header name.

```csharp
using Franz.Common.Http.Headers;

[HeaderRequired(typeof(Guid), "X-Tenant-Id")]
[HttpGet]
public IActionResult GetTenantData() => Ok();
```

Register the action-constraint capability:

```csharp
builder.Services.AddHeaderRequiredCapability();
```

The attribute is an `IActionConstraintFactory`; it resolves `HeaderRequiredActionConstraint` from DI. The package's constraint determines whether the route/action is eligible based on the declared header metadata. It is an MVC action-selection mechanism, not an authentication or authorization policy.

## Security boundary

A present header is not proof that its value is trustworthy. Validate tenant, identity, and authorization metadata against an authenticated and trusted source. Do not let clients self-assert privileged identity or tenant values simply because the action requires a header.

## Package relationship

The transport-neutral contracts and propagation configuration are in `Franz.Common.Headers`. This package supplies the ASP.NET Core implementation and MVC integration. Serialization is used to convert non-string header values through the configured `IJsonSerializer`.

## License

MIT. See [LICENSE](../../LICENSE).
