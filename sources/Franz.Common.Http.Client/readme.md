# Franz.Common.Http.Client

> A named-HttpClient-backed base service for outbound HTTP calls, request construction, serialization, and multipart content.

## Installation

```bash
dotnet add package Franz.Common.Http.Client
```

## Registration

The package exposes `AddHttpServices(IConfiguration, TimeSpan, Func<Assembly, bool>?)` in `Franz.Common.Http.Client.Extensions`.

```csharp
using Franz.Common.Http.Client.Extensions;

builder.Services.AddHttpServices(
    builder.Configuration,
    TimeSpan.FromSeconds(30),
    assembly => assembly.GetName().Name?.StartsWith("MyCompany.", StringComparison.Ordinal) == true);
```

When no predicate is supplied, discovery targets assemblies whose names start with the configured Franz company/product-generation prefix and end with `.ClientHttp`. For each matching assembly, registration derives a named client identifier from the assembly name and requires:

```json
{
  "HttpServices": {
    "MyCompany.Orders": {
      "BaseAddress": "https://orders.example.net/",
      "Timeout": "00:00:30"
    }
  }
}
```

The `BaseAddress` setting is mandatory for each discovered client. The method-level timeout is applied first; a valid per-client `Timeout` setting overrides it. Invalid or missing base-address configuration fails registration with a `TechnicalException`.

Registration also adds the package's request-builder and exception delegating handlers, registers serializers, and adds a transient `HttpClient` registration. Use the factory-managed named clients rather than creating clients manually.

## Implement an HttpService

`HttpService` is abstract. A concrete service derives from it and uses the protected request methods. Its constructor resolves the named client associated with the concrete service's assembly-derived namespace.

```csharp
using Franz.Common.Http.Client;

public sealed class OrdersClient : HttpService
{
    public OrdersClient(
        IHttpClientFactory factory,
        IEnumerable<Newtonsoft.Json.JsonConverter> converters)
        : base(factory, converters) { }

    public Task<OrderDto> GetOrderAsync(Guid id, CancellationToken ct) =>
        GetRequestAsync<OrderDto>($"orders/{id}", ct);
}
```

The base class provides GET, POST, PUT, and DELETE helpers, typed and untyped responses, cancellation-token-aware asynchronous overloads, request headers, and multipart form-data support. It also exposes query-parameter and multipart-parameter generation helpers for derived clients.

Prefer the asynchronous methods and pass the caller's cancellation token. Synchronous wrappers block on asynchronous work and should not be used in request paths.

## Request and response behavior

- Requests use the configured `HttpClient` and the package's configured JSON converters.
- Non-success responses are checked by the internal send path; inspect the concrete exception contract before translating failures at the application boundary.
- Header values are serialized before being added to `HttpRequestMessage.Headers`.
- Multipart helpers support ordinary content parameters and file parameters.
- Download helpers return a temporary-file-backed stream; dispose it to remove the backing file.

## Ownership

`HttpService` implements `IDisposable` and disposes its internally created client. Keep the service lifetime aligned with the factory-created client usage in the application. Do not share mutable request state between concurrent operations.

## Important boundaries

This package is not a generic Refit adapter and does not itself define authentication policy. Use the dedicated Refit package for typed Refit clients. Configure credentials and request headers deliberately; never place secrets in configuration files committed to source control.

## License

MIT. See [LICENSE](../../LICENSE).
