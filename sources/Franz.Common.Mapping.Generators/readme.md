# Franz.Common.Mapping.Generators

> Roslyn incremental generator for compile-time implementations of Franz IMapper contracts.

## Overview

The generator finds partial classes implementing IMapper<TSource, TDestination> and emits a partial class implementation of Map(TSource). The generated mapper performs direct property assignments and does not invoke the runtime FranzMapper.

## Installation

~~~bash
dotnet add package Franz.Common.Mapping.Generators
dotnet add package Franz.Common.Mapping
~~~

The generator is an analyzer. It runs during compilation; it is not a runtime mapping dependency.

## Usage

Define source and destination types:

~~~csharp
namespace Example;

public sealed record UserSource(Guid Id, string Name, string Email);

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
~~~

Declare a partial mapper implementing Franz's IMapper contract:

~~~csharp
using Franz.Common.Mapping.Abstractions;

public partial class UserMapper : IMapper<UserSource, UserDto>
{
    public partial UserDto Map(UserSource source);
}
~~~

The generator emits the Map implementation. Register the generated mapper through normal dependency injection:

~~~csharp
services.AddSingleton<IMapper<UserSource, UserDto>, UserMapper>();
~~~

## Matching rules

The current generator:

- matches source and destination properties by exact name;
- maps only properties whose types are exactly equal;
- ignores static properties;
- requires a destination setter (set or init); ensure the setter is accessible from the generated mapper code;
- emits an object initializer, so the destination must have an accessible parameterless constructor;
- returns default for a null source.

It does not generate type conversions, custom member expressions, nested mapping plans, collection projections, constructor selection, or runtime profile discovery. Add explicit mapping code when the property-matching rules are insufficient.

## AOT characteristics

The generated mapper consists of ordinary compiled C# and does not require runtime reflection or expression compilation for its Map operation. Native AOT compatibility still depends on the rest of the application and its dependency graph; this generator alone does not make an application AOT-safe.

## Boundaries

The generator targets the synchronous IMapper<TSource, TDestination> contract supplied by Franz.Common.Mapping.

## License

MIT. See the repository [LICENSE](../../LICENSE).
