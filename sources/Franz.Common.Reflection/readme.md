# Franz.Common.Reflection

> Assembly wrappers, assembly discovery helpers, type inspection, and host-time assembly loading.

## Overview

This package provides small reflection utilities for code that intentionally performs runtime assembly or type inspection. It does not provide a general-purpose assembly type registration API.

## Installation

~~~bash
dotnet add package Franz.Common.Reflection
~~~

## Assembly abstractions

- IAssembly exposes an underlying Assembly, Name, and FullName.
- AssemblyWrapper adapts an existing Assembly to IAssembly.
- IAssemblyAccessor exposes GetEntryAssembly().
- AssemblyAccessorWrapper returns the entry assembly, falling back to the executing assembly.

Register the accessor through:

~~~csharp
services.AddCallingAssembly();
~~~

The current AddCallingAssembly implementation registers AssemblyAccessorWrapper. Its optional Assembly parameter is retained in the signature but is not currently used by the implementation.

## Discovery helpers

ReflectionHelper provides:

- GetCurrentAppDomainAssemblies(predicate) to enumerate currently loaded, non-dynamic assemblies and optionally filter them;
- GetAssemblyCompanyOrProductPredicate(optionalPredicate) to create a predicate based on the Franz company/product assembly-name prefixes.

AssemblyExtensions.ExtractParts(IAssembly, number, joinSeparator) extracts the requested leading dot-separated parts from an assembly full name.

## Type inspection

TypeExtensions exposes:

- Implements<TInterface>() to test whether a type implements an interface;
- DirectlyImplements<TInterface>() to distinguish interfaces introduced by the type from those inherited from its base class.

These methods inspect runtime interface metadata and are annotated with RequiresUnreferencedCode. Preserve the relevant metadata when using them in trimmed applications.

## Host-time assembly loading

The IHostBuilder extension LoadAssemblyReferencedNotLoaded(entryAssembly) loads matching Franz assemblies from AppContext.BaseDirectory when they are not already loaded. The method uses filesystem discovery and runtime assembly loading.

It is annotated with RequiresUnreferencedCode and RequiresDynamicCode. It is not suitable for Native AOT or a fully trimmed deployment. Prefer explicit compile-time registration when possible.

## Registration boundary

Use the explicit registration APIs from the package that owns each service contract. This package supplies reflection utilities rather than a general service-scanning convention.

## License

MIT. See the repository [LICENSE](../../LICENSE).
