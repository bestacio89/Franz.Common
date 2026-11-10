# Franz.Common.DependencyInjection

> Assembly scanning and explicit service-registration helpers built on Microsoft DI and Scrutor.

## Overview

Assembly scanning and explicit service-registration helpers built on Microsoft DI and Scrutor.

## Installation

```bash
dotnet add package Franz.Common.DependencyInjection
```

## Marker-based registration

The package recognizes `IScopedDependency` and `ISingletonDependency` markers from `Franz.Common`. The marker interface itself does not register a service; invoke the corresponding extension and ensure the implementation assembly is included by the package's assembly selector.

```csharp
using Franz.Common.DependencyInjection;
using Franz.Common.DependencyInjection.Extensions;

services.AddScopedDependencies();
services.AddSingletonDependencies();
```

`AddDependencies()` composes both marker scans. Each scan registers implemented interfaces other than the marker interface and uses `RegistrationStrategySkipExistingPair`.

## Assembly selection

The scanning methods accept an optional `Func<Assembly, bool>` predicate. The underlying `FromCompanyApplicationDependenciesWithPredicate` selector determines the candidate assemblies; the predicate narrows that set.

```csharp
services.AddDependencies(
    assembly => assembly.GetName().Name?.StartsWith("Contoso.", StringComparison.Ordinal) == true);
```

Use explicit, stable assembly-selection rules. A broad scan can register infrastructure from test, tooling, or unrelated application assemblies.

## Explicit scanning extensions

The package also exposes these Scrutor-backed helpers:

| Extension | Registration shape |
|---|---|
| `AddSelfScoped<TInterface>` | Matching implementations as themselves, scoped |
| `AddMatchingInterfaceScoped<TInterface>` | Matching interface, scoped |
| `AddImplementedInterfaceScoped<TInterface>` | All implemented interfaces, scoped |
| `AddImplementedInterfaceTransient<TInterface>` | All implemented interfaces, transient |
| `AddImplementedInterfaceSingleton<TInterface>` | All implemented interfaces, singleton |
| `AddImplementedInterfaceWithoutMarkerScoped<TInterface>` | Implemented interfaces excluding the marker type, scoped |
| `AddImplementedInterfaceWithoutMarkerSingleton<TInterface>` | Implemented interfaces excluding the marker type, singleton |
| `AddInheritedClassSingleton<TType>` | Register matching classes as `TType`, singleton |

These methods scan assemblies selected by `FromCompanyApplicationDependenciesWithPredicate`; they are not global scans of every loaded assembly.

## Native AOT and trimming

The Scrutor assembly-scanning extensions (`AddDependencies`, `AddScopedDependencies`, `AddSelfScoped`, and the other scan-based helpers) are explicitly marked with `RequiresUnreferencedCode`. They discover arbitrary assemblies and implementation types at runtime, so they are intended for JIT applications and are not part of the Native AOT contract. Native AOT callers should not suppress those warnings.

Use explicit generic registrations instead:

```csharp
services.AddNoDuplicateSingleton<IClock, SystemClock>();
services.AddNoDuplicateScoped<IOrderRepository, OrderRepository>();
```

The generic implementation-type overloads preserve public constructors through `DynamicallyAccessedMembers` annotations. Type-based overloads carry the same constructor-preservation requirement on their `Type` parameter. Factory and instance registrations remain explicit and do not require assembly discovery.

The package's Native AOT smoke suite publishes and executes a consumer that uses explicit generic registration. It deliberately does not call assembly-scanning APIs.

## Duplicate-registration semantics

`RegistrationStrategySkipExistingPair` compares both `ServiceType` and `ImplementationType`. An existing matching pair is retained. If the same pair is already registered with a different lifetime, registration throws rather than silently preserving an inconsistent lifetime.

The package also exposes `AddNoDuplicateService` and typed `AddNoDuplicateSingleton`, `AddNoDuplicateScoped`, and `AddNoDuplicateTransient` overloads for implementation types, instances, and factories. These helpers use the same pair-based strategy. They do not deduplicate registrations merely because the service type matches when the implementation differs.

## Lifetime design

- Scoped services must not be captured by singleton services.
- Singleton implementations must be thread-safe and must not retain request-specific state.
- Transient registrations create a new instance per resolution, subject to the consuming container's normal semantics.

## Namespace

All extension methods are in `Franz.Common.DependencyInjection.Extensions`. The custom strategy is `Franz.Common.DependencyInjection.RegistrationStrategySkipExistingPair`.

## License

MIT. See [LICENSE](../../LICENSE).
