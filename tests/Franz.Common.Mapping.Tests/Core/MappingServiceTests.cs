using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;
using Franz.Common.Mapping.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Mapping.Tests.Core;

// Dummy classes for generic types
public class User { public string Name { get; set; } = string.Empty; }
public class UserDto { public string Name { get; set; } = string.Empty; }
public class Product { public decimal Price { get; set; } }
public class ProductDto { public decimal Price { get; set; } }

// 1. Explicit Source-Generated Mapper logic simulating IAsyncMapper<TSource, TDestination>
public class UserMapper : IAsyncMapper<User, UserDto>, IMapper<User, UserDto>
{
    private static int _instantiations;
    public static int Instantiations => _instantiations;

    public UserMapper() => Interlocked.Increment(ref _instantiations);

    public UserDto Map(User source) => new() { Name = source.Name };
    public ValueTask<UserDto> MapAsync(User source, CancellationToken cancellationToken = default) 
        => new(new UserDto { Name = source.Name });

    public static void Reset() => _instantiations = 0;
}

// 2. Implicit AutoMapper test class (no specific mapper interface registered manually)
public class ImplicitProfile : FranzMapProfile
{
    public ImplicitProfile()
    {
        CreateMap<Product, ProductDto>();
    }
}

public class MappingServiceTests
{
    [Fact]
    public async Task MappingService_Should_ResolveAndCacheMappers_Concurrently_WithoutRaceConditions()
    {
        // Arrange
        UserMapper.Reset();
        var services = new ServiceCollection();
        
        // Setup Dependency Injection correctly
        services.AddTransient<IAsyncMapper<User, UserDto>, UserMapper>();
        services.AddSingleton<MappingService>();
        
        var provider = services.BuildServiceProvider();
        var mappingService = provider.GetRequiredService<MappingService>();

        // Act - Hammer IMappingService with Task.WhenAll
        var tasks = Enumerable.Range(0, 1000).Select(async _ =>
        {
            var user = new User { Name = "Concurrent" };
            return await mappingService.MapAsync<User, UserDto>(user);
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(1000);
        results.All(r => r.Name == "Concurrent").Should().BeTrue();
        
        // Strict Double-Check locking validation: The DI transient service 
        // should only be requested and instantiated exactly ONCE because it gets cached!
        UserMapper.Instantiations.Should().Be(1);
    }

    [Fact]
    public void MappingService_Should_Resolve_DualParadigm_Safely()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // We use MapExtensions to load the assemblies so it finds ImplicitProfile + UserMapper automatically
        services.AddFranzMapping(typeof(MappingServiceTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mappingService = provider.GetRequiredService<IMappingService>();

        var user = new User { Name = "Explicit" };
        var product = new Product { Price = 42m };

        // Act
        // Explicitly found Mapper
        var userDto = mappingService.Map<User, UserDto>(user);
        
        // Implicitly built Mapper via Fallback
        var productDto = mappingService.Map<Product, ProductDto>(product);

        // Assert
        userDto.Should().NotBeNull();
        userDto.Name.Should().Be("Explicit");

        productDto.Should().NotBeNull();
        productDto.Price.Should().Be(42m);
    }

    [Fact]
    public async Task MappingService_DisposeAsync_ShouldHandle_GracefulTeardown()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MappingService>();
        
        var mappingService = services.BuildServiceProvider().GetRequiredService<MappingService>();
        
        // Should not throw
        await mappingService.DisposeAsync();

        // Should throw ObjectDisposedException
        var act = async () => await mappingService.MapAsync<User, UserDto>(new User());
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}
