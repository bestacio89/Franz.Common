# Franz.Common.AutoMapper

> Assembly-scoped registration of AutoMapper profiles through Microsoft dependency injection.

## Installation

```bash
dotnet add package Franz.Common.AutoMapper
```

## Registration API

The package exposes `AddAutoMapper` in the `Franz.Common.AutoMapper.Extensions` namespace. It accepts an optional array of assemblies to scan.

```csharp
using System.Reflection;
using Franz.Common.AutoMapper.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Prefer explicit assemblies so registration is deterministic.
builder.Services.AddAutoMapper(
    typeof(OrderMappingProfile).Assembly,
    typeof(CustomerMappingProfile).Assembly);
```

When no assemblies are supplied, the extension uses `Assembly.GetCallingAssembly()`. Explicit assembly arguments are recommended in layered applications, test hosts, and applications where extension-call placement could make the calling assembly ambiguous.

## Define profiles

Profiles are standard AutoMapper profiles. The package delegates profile discovery and registration to AutoMapper's Microsoft DI integration.

```csharp
using AutoMapper;

public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<CreateOrderDto, Order>();
    }
}
```

## Composition guidance

- Call registration once during service composition and pass the assemblies that contain the profiles.
- Keep profiles in the application or contracts boundary that owns the mapping.
- Validate mappings in tests with AutoMapper's configuration validation.
- Do not expect this package to generate mappings or provide a custom mapper abstraction; it is an AutoMapper registration adapter.

## API and namespace

```csharp
namespace Franz.Common.AutoMapper.Extensions;

public static IServiceCollection AddAutoMapper(
    this IServiceCollection services,
    params Assembly[] assemblies);
```

## License

MIT. See [LICENSE](../../LICENSE).
