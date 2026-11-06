# Franz.Common.Bootstrap

> A composition extension that registers the common Franz application architecture for a selected set of assemblies.

## Overview

A composition extension that registers the common Franz application architecture for a selected set of assemblies.

## Installation

```bash
dotnet add package Franz.Common.Bootstrap
```

## Composition entry point

The current public entry point is `AddCommonArchitecture`, defined in `Franz.Common.Bootstrap.Extensions`. It accepts application configuration and an optional set of assemblies.

```csharp
using Franz.Common.Bootstrap.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommonArchitecture(
    builder.Configuration,
    typeof(Program).Assembly);
```

If no assembly is supplied, the extension falls back to the calling assembly. Prefer explicit assemblies for deterministic composition and to avoid depending on where the extension is invoked.

## What it registers

For each supplied assembly, the extension:

1. Calls `AddCallingAssembly(assembly)` from `Franz.Common.Reflection`.
2. Calls `AddFranzMediator(new[] { assembly })` from `Franz.Common.Mediator.Extensions`.
3. Calls `AddDependencies()` from `Franz.Common.DependencyInjection.Extensions`.

The dependency scanner therefore applies the marker-based conventions of the dependency-injection package. Mediator handler and pipeline registration is delegated to the mediator package.

## What it does not register

The implementation does not currently call AutoMapper registration, logging registration, hosting initialization registration, persistence registration, messaging transport registration, or HTTP middleware registration. Add those capabilities explicitly through their owning packages.

The `IConfiguration` parameter is part of the current method signature, but the implementation does not currently read values from it. Do not assume that configuration sections are bound by this call.

## Recommended composition

Keep the composition root explicit. Use this helper for the common reflection, mediator, and marker-based dependency registration, then add provider-specific infrastructure and host lifecycle services separately.

## API

```csharp
IServiceCollection AddCommonArchitecture(
    this IServiceCollection services,
    IConfiguration configuration,
    params Assembly[] assemblies);
```

Namespace: `Franz.Common.Bootstrap.Extensions`.

## License

MIT. See [LICENSE](../../LICENSE).
