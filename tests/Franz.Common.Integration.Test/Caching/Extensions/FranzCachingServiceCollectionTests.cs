using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

public class FranzCachingServiceCollectionExtensionsTests
{
  [Fact]
  public void AddFranzCaching_Should_Register_Default_Memory_Provider()
  {
    var services = new ServiceCollection();
    services.AddFranzCaching();

    using var provider = services.BuildServiceProvider();
    provider.GetRequiredService<ICacheProvider>().Should().NotBeNull();
    provider.GetRequiredService<ISettingsCache>().Should().NotBeNull();
    provider.GetRequiredService<ICacheKeyStrategy>().Should().NotBeNull();
  }

  [Fact]
  public void AddFranzMediatorCaching_Should_Add_Pipeline()
  {
    var services = new ServiceCollection();
    services.AddFranzMediatorCaching();

    var registrations = services.FirstOrDefault(s =>
        s.ImplementationType?.Name.Contains("CachingPipeline") ?? false);

    registrations.Should().NotBeNull();
  }
}
