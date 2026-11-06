# Franz.Common.Mapping.Generators

> Incremental Roslyn source generator for strongly typed, compile-time mapper implementations.

## Overview

The generator emits an implementation for a partial mapper class implementing `IMapper<TSource, TDestination>`. It is included with the `Franz.Common.Mapping` package and normally requires no separate application reference.

## Installation

```bash
dotnet add package Franz.Common.Mapping
```

## Define a mapper

```csharp
namespace Example;

using Franz.Common.Mapping.Abstractions;

public partial class CustomerMapper : IMapper<Customer, CustomerDto>
{
    public partial CustomerDto Map(Customer source);
}
```

The generator discovers top-level partial classes in a named namespace. For `IMapper<TSource, TDestination>`, it emits the matching `Map(TSource source)` implementation. The `IMapper<TSource, TDestination>` contract is supplied by `Franz.Common.Mapping`.

## Mapping rules

The current generator:

- Matches source and destination properties by exact name and identical type. The generated mapper must be able to access the source getters and destination setters or the consuming compilation will fail.
- Emits a destination object initializer containing the matched properties.
- Returns the default destination value when the source is null.
- Does not perform type conversion, nested mapping, collection projection, custom profile lookup, or constructor-parameter binding.

The mapper must be a public, top-level, non-generic partial class in a named namespace because the generated declaration is `public partial class` in that namespace. The destination must have an accessible parameterless constructor and the generated mapper must be able to access the matched property setters. Use the runtime mapper for configured constructor/member mapping where trimming and Native AOT are not required.

The package exposes a synchronous generated mapping contract. It does not ship an asynchronous mapper contract or an asynchronous mapping pipeline.

## AOT boundary

The generated mapper path avoids runtime reflection and dynamic expression compilation for the generated method. The runtime `IFranzMapper`/`IMappingService` path is reflection/dynamic-code based and is not covered by this generator's AOT guarantee.

## License

MIT. See the repository [LICENSE](../../LICENSE).
