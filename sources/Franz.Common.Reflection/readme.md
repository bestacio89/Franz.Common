# Franz.Common.Reflection

> Focused reflection helpers, assembly wrappers, type-inspection extensions, and host assembly loading.

## Overview

This package contains small reflection abstractions and helpers. It does not provide a general-purpose assembly scanning DI container or a service-registration convention engine.

## Installation

```bash
dotnet add package Franz.Common.Reflection
```

## Assembly abstractions

- `IAssembly` exposes the wrapped assembly's name and full name.
- `AssemblyWrapper` adapts a `System.Reflection.Assembly`.
- `IAssemblyAccessor` exposes the entry assembly abstraction.
- `AssemblyAccessorWrapper` resolves `Assembly.GetEntryAssembly()`, falling back to the executing assembly.

`AddCallingAssembly(assembly)` registers an `IAssemblyAccessor`. The optional assembly parameter is currently not used to select the returned assembly; the accessor resolves the process entry assembly. Do not rely on that argument to override the entry assembly.

## Host assembly loading

`LoadAssemblyReferencedNotLoaded(entryAssembly)` scans `AppContext.BaseDirectory` for DLL names matching the first two name components of the supplied entry assembly and loads those not already loaded.

This operation is explicitly marked as trimming- and Native AOT-incompatible because it discovers and loads assemblies at runtime. Prefer explicit compile-time registration and generated manifests in trimmed/AOT applications.

## Type and assembly helpers

- `ReflectionHelper.GetCurrentAppDomainAssemblies(predicate)` returns loaded, non-dynamic assemblies, optionally filtered.
- `ReflectionHelper.GetAssemblyCompanyOrProductPredicate(optionalPredicate)` creates a filter for matching company/product assembly names.
- `Type.Implements<TInterface>()` checks whether a type implements the specified interface.
- `Type.DirectlyImplements<TInterface>()` excludes interfaces inherited from the base class when evaluating direct implementation.
- `IAssembly.ExtractParts(number, joinSeparator)` returns the requested leading components of the wrapped assembly's full name and throws when the name has too few components.

## License

MIT. See the repository [LICENSE](../../LICENSE).
