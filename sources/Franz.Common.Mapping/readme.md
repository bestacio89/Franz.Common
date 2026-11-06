# Franz.Common.Mapping

> Runtime mapping configuration and execution contracts, with an optional compile-time mapper generator.

## Overview

Franz.Common.Mapping provides a configured runtime mapper and a small set of explicit mapping contracts. It separates mapping configuration, execution, and application-facing service access.

The runtime mapper uses reflection-based model inspection and runtime mapping-plan construction. It is not the Native AOT path. Use generated IMapper<TSource, TDestination> implementations for trimming/AOT-sensitive code.

## Installation

~~~bash
dotnet add package Franz.Common.Mapping
~~~

## Runtime mapping registration

~~~csharp
using Franz.Common.Mapping.Extensions;

services.AddFranzMapping(
    assemblies: new[] { typeof(ApplicationMappingProfile).Assembly });
~~~

AddFranzMapping discovers concrete IFranzMapProfile implementations in the selected assemblies, invokes their parameterless constructors, applies their mapping registrations, and registers MappingConfiguration, IFranzMapper, and IMappingService.

If no assemblies are supplied, the helper scans the current AppDomain for matching application assemblies. Prefer explicit assembly selection to keep discovery scope predictable.

## Mapping profiles

Derive a profile from FranzMapProfile and register mappings with CreateMap:

~~~csharp
public sealed class ApplicationMappingProfile : FranzMapProfile
{
    public ApplicationMappingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(destination => destination.Number, source => source.OrderNumber)
            .Ignore(destination => destination.InternalNotes)
            .Strict();

        CreateMap<CreateOrderRequest, Order>()
            .ConstructUsing(request => new Order(request.CustomerId, request.Lines));
    }
}
~~~

The fluent MappingExpression API supports:

- ForMember(destination, source) for explicit member binding;
- Ignore(destination) to exclude a destination member;
- ConstructUsing(...) for an explicit destination constructor;
- ReverseConstructUsing(...) for a reverse constructor;
- Strict() to enable strict mapping validation.

MappingConfiguration.Register uses last-registration-wins behavior for a source/destination pair.

## Execution

Use IFranzMapper.Map<TSource, TDestination> for direct runtime mapping or IMappingService for DI-oriented orchestration. MappingService.MapAsync returns ValueTask but mapping remains CPU-bound; it does not turn mapping into asynchronous I/O.

The runtime engine supports configured mappings and its fallback mapping behavior. Do not assume that an unconfigured mapping is equivalent to a fully explicit profile or that every source/destination pair is suitable for fallback mapping.

## Native AOT and trimming

AddFranzMapping and IFranzMapper/IMappingService runtime mapping APIs are annotated with RequiresUnreferencedCode and RequiresDynamicCode. Assembly profile discovery and runtime mapping plans are not a warning-clean Native AOT path.

For AOT-sensitive code, implement IMapper<TSource, TDestination> with the optional Franz.Common.Mapping.Generators package and register that generated mapper directly. Do not rely on runtime profile scanning or IFranzMapper for the generated path.

## Related package

- Franz.Common.Mapping.Generators — compile-time generation of strongly typed IMapper implementations.

## License

MIT. See the repository [LICENSE](../../LICENSE).
