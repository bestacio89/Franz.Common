# Franz.Common.Hosting

> Ordered asynchronous initialization of scoped application services during host startup.

## Overview

Ordered asynchronous initialization of scoped application services during host startup.

## Installation

```bash
dotnet add package Franz.Common.Hosting
```

## Initializer contract

Implement `IHostingInitializer` for startup work that must run after the host's service provider is available.

```csharp
using Franz.Common.Hosting;

public sealed class ReferenceDataInitializer : IHostingInitializer
{
    public int Order => 100;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Perform bounded, idempotent initialization.
        return Task.CompletedTask;
    }
}
```

The interface inherits `IScopedDependency`, so implementations participate in the marker-based scoped registration convention when their assemblies are scanned by `Franz.Common.DependencyInjection`.

## Initialization lifecycle

The package provides:

- `IServiceProvider.InitializeAsync()`: creates a scope, resolves all `IHostingInitializer` implementations, orders them by ascending `Order`, and awaits them sequentially.
- `IHost.Initialize()`: validates the host, invokes `host.Services.InitializeAsync()`, then returns the same host instance.

```csharp
using Franz.Common.Hosting.Extensions;

var host = builder.Build();

await host.Initialize();
await host.RunAsync();
```

Initialization is sequential, not parallel. The implementation does not pass a cancellation token to each initializer; each initializer receives its optional token through its own contract, but the orchestration method currently invokes the parameterless call. Initializers should therefore be bounded, idempotent where possible, and explicit about their own cancellation and failure behavior.

If an initializer throws, the sequential loop stops and the exception propagates to the caller. The host should not proceed to serving traffic after failed mandatory initialization.

## Scope and disposal

All initializers resolved by one call share the created scope. The scope is disposed when initialization completes, including when an initializer fails. Do not retain scoped dependencies beyond this lifecycle.

## Registration

Register initializers through the scoped marker scan or explicitly as scoped services. The package does not scan assemblies or automatically invoke initialization merely because the host is built.

## License

MIT. See [LICENSE](../../LICENSE).
