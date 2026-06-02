using FluentAssertions;
using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;
using Franz.Common.Mapping.Extensions;
using Franz.Common.Mapping.Profiles;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Mapping.Tests.Core;

// =========================================================
// EXPLICIT MAPPER
// =========================================================
public class UserMapper : IMapper<User, UserDto>
{
  public UserDto Map(User source)
      => new() { Name = source.Name };

  public ValueTask<UserDto> MapAsync(User source, CancellationToken cancellationToken = default)
      => new(new UserDto { Name = source.Name });
}

// =========================================================
// IMPLICIT PROFILE (Franz fallback mapping)
// =========================================================
public class ImplicitProfile : FranzMapProfile
{
  public ImplicitProfile()
  {
    CreateMap<Product, ProductDto>();
  }
}

// =========================================================
// TEST SUITE
// =========================================================
public class MappingServiceTests
{
  // -----------------------------------------------------
  // 1. Concurrency safety
  // -----------------------------------------------------
  [Fact]
  public async Task MappingService_Should_BeThreadSafe_UnderHighConcurrency()
  {
    var services = new ServiceCollection();
    services.AddFranzMapping(typeof(MappingServiceTests).Assembly);

    services.AddSingleton<MappingService>();

    var provider = services.BuildServiceProvider();
    var mappingService = provider.GetRequiredService<MappingService>();

    var tasks = Enumerable.Range(0, 1000)
    .Select(_ =>
        mappingService
            .MapAsync<User, UserDto>(new User { Name = "Concurrent" })
            .AsTask());

    var results = await Task.WhenAll(tasks);

    results.Should().HaveCount(1000);
    results.Should().OnlyContain(r => r.Name == "Concurrent");
  }

  // -----------------------------------------------------
  // 2. Dual paradigm (explicit + implicit mapping)
  // -----------------------------------------------------
  [Fact]
  public void MappingService_Should_Handle_ExplicitAndImplicit_Mapping()
  {
    var services = new ServiceCollection();

 

    // implicit profile system
    services.AddFranzMapping(typeof(MappingServiceTests).Assembly);

    services.AddSingleton<MappingService>();

    var provider = services.BuildServiceProvider();
    var mappingService = provider.GetRequiredService<IMappingService>();

    var user = new User { Name = "Explicit" };
    var product = new Product { Price = 42m };

    var userDto = mappingService.Map<User, UserDto>(user);
    var productDto = mappingService.Map<Product, ProductDto>(product);

    userDto.Should().NotBeNull();
    userDto.Name.Should().Be("Explicit");

    productDto.Should().NotBeNull();
    productDto.Price.Should().Be(42m);
  }

  // -----------------------------------------------------
  // 3. Dispose safety
  // -----------------------------------------------------
  [Fact]
  public async Task MappingService_DisposeAsync_Should_Block_FurtherCalls()
  {
    var services = new ServiceCollection();
    services.AddFranzMapping(typeof(MappingServiceTests).Assembly);
    services.AddSingleton<MappingService>();

    var provider = services.BuildServiceProvider();
    var mappingService = provider.GetRequiredService<MappingService>();

    await mappingService.DisposeAsync();

    Func<Task> act = async () =>
        await mappingService.MapAsync<User, UserDto>(
            new User { Name = "Test" });

    await act.Should().ThrowAsync<ObjectDisposedException>();
  }
}