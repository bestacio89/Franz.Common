# Franz.Common.Mapping

> Configurable runtime mapping plus strongly typed mapping contracts for application and domain models.

## Overview

The package exposes two distinct mapping paths:

1. `IFranzMapper`/`IMappingService`: a runtime mapper driven by `MappingConfiguration` and `IFranzMapProfile`.
2. `IMapper<TSource, TDestination>`: a strongly typed contract implemented by the companion incremental source generator.

These paths have different trimming and Native AOT characteristics.

## Installation

```bash
dotnet add package Franz.Common.Mapping
```

The mapping generator is included as an analyzer dependency of the package.

## Runtime mapper registration

```csharp
using Franz.Common.Mapping.Extensions;

builder.Services.AddFranzMapping(
    assemblies: new[] { typeof(CustomerMapProfile).Assembly });
```

The extension registers `MappingConfiguration`, `IFranzMapper`, and `IMappingService`. It discovers concrete `IFranzMapProfile` implementations in the supplied assemblies; when no assemblies are supplied, it scans matching assemblies loaded in the current AppDomain. This profile discovery uses reflection and dynamic activation and is annotated as trimming/Native AOT incompatible.

## Profiles and expressions

Derive a profile from `FranzMapProfile` and declare mappings through `CreateMap<TSource, TDestination>()`:

```csharp
public sealed class CustomerMapProfile : FranzMapProfile
{
    public CustomerMapProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(destination => destination.DisplayName, source => source.Name)
            .Ignore(destination => destination.InternalCode)
            .Strict();
    }
}
```

The expression API supports:

- `ForMember(destination, source)` for direct property-to-property bindings.
- `Ignore(destination)` to skip a destination member.
- `ConstructUsing(Func<TSource, TDestination>)` or the mapper-aware overload.
- `ReverseConstructUsing(...)` stores a reverse constructor delegate in the expression. The current runtime mapper does not consume that delegate or expose automatic reverse mapping.
- `Strict()` to fail when required source members/constructor parameters cannot be resolved.

Mappings are registered by source/destination type pair; a later registration for the same pair replaces the previous expression.

## Runtime behavior

The runtime mapper supports constructor-based destination creation, explicit constructor selection through `[MapConstructor]`, member bindings, ignored members, collection mapping, and `[ValueObject]` unwrapping. It detects circular object graphs and fails with a technical mapping exception.

`IMappingService.MapAsync` is a compatibility/composition API that returns a `ValueTask`; mapping remains CPU-bound and is not moved to a background thread.

## Native AOT

The reflection-based `IFranzMapper` and profile-discovery registration are not Native AOT-safe. For trimming and Native AOT, use a generated `IMapper<TSource, TDestination>` implementation from the companion source generator and call its typed `Map` method directly. See [Franz.Common.Mapping.Generators](../Franz.Common.Mapping.Generators/readme.md).

## License

MIT. See the repository [LICENSE](../../LICENSE).
