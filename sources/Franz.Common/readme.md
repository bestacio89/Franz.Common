# Franz.Common

> Foundational contracts and small cross-cutting utilities shared by Franz.Common applications.

## Overview

Foundational contracts and small cross-cutting utilities shared by Franz.Common applications.

## Responsibilities

This package contains framework-neutral building blocks. It is not the dependency-injection scanner, hosting bootstrapper, mediator implementation, or persistence provider. Those capabilities live in their dedicated packages.

The current source surface includes:

- Dependency lifetime markers: `IScopedDependency` and `ISingletonDependency`.
- Seeding contract: `ISeeder`.
- Collection helpers: `AddIfNotContains`, `AddRange`, and `IsNullOrEmpty`.
- Enumerable helper: `ForEach`.
- Environment predicates: `IsIntegration`, `IsValidation`, and `IsPreProduction`.
- Shared company/product-generation identifiers and resource definitions.

## Installation

```bash
dotnet add package Franz.Common
```

## Dependency lifetime markers

The marker interfaces describe intended lifetimes for implementations discovered by the separate `Franz.Common.DependencyInjection` package. They do not register services by themselves.

```csharp
using Franz.Common.DependencyInjection;

public sealed class RequestContext : IScopedDependency
{
    // Request-scoped state belongs here.
}

public sealed class ApplicationClock : ISingletonDependency
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

When using assembly scanning, include the implementation assemblies in the scanner's discovery scope and apply the matching registration extensions from `Franz.Common.DependencyInjection`. Do not mark a service singleton if it captures scoped dependencies or mutable request state.

## Seeding contract

`ISeeder` exposes an ordering value and asynchronous seed operation. The application owns orchestration, scope creation, execution policy, and failure handling; this package does not automatically run seeders.

```csharp
using Franz.Common.Data;

public sealed class ReferenceDataSeeder : ISeeder
{
    public int Order => 100;

    public Task SeedAsync()
    {
        // Seed application-owned reference data.
        return Task.CompletedTask;
    }
}
```

Resolve seeders from an application scope, order them by `Order`, and await each operation. Decide explicitly whether a failure should stop subsequent seeders.

## Utility extensions

Collection and enumerable extensions are convenience operations; they do not introduce persistence, concurrency, or transactional guarantees. Environment predicates are helpers over the host environment name. Use the host's canonical environment configuration and avoid treating these predicates as a security boundary.

## Package boundaries

| Concern | Package |
|---|---|
| Marker-based service scanning | `Franz.Common.DependencyInjection` |
| Host initialization lifecycle | `Franz.Common.Hosting` |
| Mediator dispatch and pipelines | `Franz.Common.Mediator` |
| HTTP composition | `Franz.Common.Http.Bootstrap` |
| Persistence and repositories | `Franz.Common.EntityFramework` and provider packages |

## Compatibility and maintenance

The package targets the repository's .NET 10 baseline. The package version is governed by the repository's release process; do not infer a package version from historical text in this guide.

## License

MIT. See [LICENSE](../../LICENSE).
