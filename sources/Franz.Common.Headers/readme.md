# Franz.Common.Headers

> Transport-agnostic header contracts and configuration for propagating selected headers.

## Installation

```bash
dotnet add package Franz.Common.Headers
```

## Header context contract

`IHeaderContextAccessor` exposes headers in a serialization-friendly representation:

```csharp
public interface IHeaderContextAccessor
{
    IDictionary<string, string[]> ListAll();
    bool TryGetValue(string key, out string[]? value);
    bool TryGetValue<T>(string key, out T value);
}
```

The raw overload returns all values as a string array. The generic overload attempts to deserialize the value to the requested type; consumers should handle a failed conversion explicitly. The interface is transport-agnostic and does not depend on ASP.NET Core's `StringValues`.

## Header propagation registration

The package exposes `AddHeaderPropagation(HeaderPropagationSetting)` in `Franz.Common.Headers.Extensions`.

```csharp
using Franz.Common.Headers;
using Franz.Common.Headers.Extensions;

var setting = new HeaderPropagationSetting();
// Configure the setting's propagation entries for the application.
services.AddHeaderPropagation(setting);
```

Registration adds `IHeaderPropagationRegistrer` using a no-duplicate singleton registration and registers the supplied setting as `IHeaderPropagationSetting`. The settings instance is registered as a singleton; configure it fully before registration and do not mutate it concurrently at runtime.

## Configuration model

- `HeaderPropagationOptions.Headers` is an initialized collection of header names.
- `HeaderPropagationSetting` is the concrete configuration supplied at registration.
- `IHeaderPropagationSetting` and `IHeaderPropagationRegistrer` define the contracts used by the propagation integration.
- `HeaderConstants` centralizes header-name constants.

This package does not itself add ASP.NET Core middleware or configure an `IHttpClientFactory` pipeline. HTTP-specific integration belongs to the HTTP packages.

## Security and correctness

Propagate only headers explicitly required by downstream services. Never blindly forward credentials, cookies, or untrusted identity claims across trust boundaries. Normalize header-name handling consistently and define behavior for missing, repeated, and malformed values.

## License

MIT. See [LICENSE](../../LICENSE).
