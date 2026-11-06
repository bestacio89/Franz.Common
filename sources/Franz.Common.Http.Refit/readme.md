# Franz.Common.Http.Refit

> Refit typed-client registration with Franz header and optional authentication handlers.

## Installation

```bash
dotnet add package Franz.Common.Http.Refit
```

## Register a typed client

The current extension is `AddFranzRefit<TClient>` in `Franz.Common.Http.Refit.Extensions`.

```csharp
using Franz.Common.Http.Refit.Extensions;

builder.Services.AddFranzRefit<IBooksApi>(
    name: "Books",
    baseUrl: configuration["ExternalApis:Books:BaseUrl"]!,
    policyName: "books-read",
    configureOptions: options =>
    {
        options.EnableOpenTelemetry = true;
        options.Timeout = TimeSpan.FromSeconds(20);
        options.DefaultPolicyName = "books-read";
    });
```

The method registers the Refit client, configures its base address, adds `FranzRefitHeadersHandler`, and adds `FranzRefitAuthHandler`. A named Polly policy is attached only when the `policyName` argument is non-empty and the host has registered that policy in the Polly registry. The `configureRefitSettings` callback can customize Refit's own settings independently of Franz options.

**Implementation caveat:** the current extension accepts a `name` argument but does not pass it to a named Refit-client registration. Do not rely on that argument to establish a distinct named `HttpClient` until the implementation is corrected.

## Authentication

Implement `ITokenProvider` to supply a token asynchronously:

```csharp
using Franz.Common.Http.Refit.Contracts;

public sealed class ServiceTokenProvider : ITokenProvider
{
    public Task<string?> GetTokenAsync(CancellationToken ct = default)
    {
        // Retrieve a token from the application's secure token service.
        return Task.FromResult<string?>(null);
    }
}
```

Register the provider in DI. When no provider is registered, the extension creates a private no-op provider that returns null. Do not interpret this as authentication being configured; no Authorization token is produced by the no-op provider.

## Options contract

`RefitClientOptions` exposes:

| Option | Default | Purpose |
|---|---:|---|
| `Timeout` | null | Optional timeout value |
| `EnableOpenTelemetry` | true | Enables HTTP activity tagging behavior where implemented |
| `DefaultPolicyName` | null | Default policy name |
| `AutoHandleAuthFailures` | true | Authentication recovery switch |
| `MaxAuthRetryAttempts` | 1 | Maximum recovery attempts |
| `OnAuthenticationFailureAsync` | null | Async credential-refresh callback |
| `OnAuthRecoverySuccess` | null | Recovery-success callback |
| `OnAuthRecoveryFailed` | null | Recovery-failure callback |

The current extension registers configured options through `services.Configure(configureOptions)`. Its authentication-handler factory also requests `RefitClientOptions` directly from DI rather than resolving `IOptions<RefitClientOptions>`; verify this path before relying on options-driven authentication recovery. The timeout and default-policy option properties are not automatically equivalent to the explicit `policyName` argument.

## Typed API contract

```csharp
using Refit;

public interface IBooksApi
{
    [Get("/books/{id}")]
    Task<ApiResponse<BookDto>> GetBookAsync(Guid id);
}
```

## Operational guidance

Register only the headers and credentials that the downstream trust boundary requires. Define retry policies according to operation idempotency; retries on non-idempotent requests can duplicate side effects. The host remains responsible for configuring OpenTelemetry providers/exporters and for securing token acquisition.

## License

MIT. See [LICENSE](../../LICENSE).
