using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions; 
using Franz.Common.Caching.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Franz.Common.Caching.Tests.Extensions;

public sealed class AddFranzRedisCachingTests
{
  [Fact]
  public void Should_Register_Redis_Provider_With_ConnectionString()
  {
    // Arrange & Act
    // We use the Action-only overload we created for Fixtures/Tests
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddFranzRedisCaching(options =>
    {
      options.ConnectionString = "localhost:6379";
      options.KeyPrefix = "test:";
    });

    var sp = services.BuildServiceProvider();

    // Assert
    // Verify the provider is registered correctly
    sp.GetRequiredService<ICacheProvider>()
      .Should().BeOfType<RedisCacheProvider>();

    // Verify the Options contain our string
    var options = sp.GetRequiredService<IOptions<CacheOptions>>().Value;
    options.ConnectionString.Should().Be("localhost:6379");
  }

  [Fact]
  public void Should_Register_Redis_Provider_Using_Existing_Multiplexer()
  {
    // Arrange
    var muxerMock = new Mock<IConnectionMultiplexer>();
    var services = new ServiceCollection();
    services.AddLogging();

    // 1. Manually register the multiplexer (simulating a factory/existing instance)
    services.AddSingleton(muxerMock.Object);

    // 2. Register Redis caching
    // The RegisterRedisInternal logic uses TryAddSingleton, 
    // so it will respect the existing muxer.
    services.AddFranzRedisCaching(options => {
      options.ConnectionString = "unused-but-required-for-validation";
    });

    var sp = services.BuildServiceProvider();

    // Assert
    var provider = sp.GetRequiredService<ICacheProvider>();
    provider.Should().BeOfType<RedisCacheProvider>();

    sp.GetRequiredService<IConnectionMultiplexer>()
      .Should().Be(muxerMock.Object);
  }

  [Fact]
  public void AddFranzMemoryCaching_Should_Register_MemoryProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddFranzMemoryCaching();

    var sp = services.BuildServiceProvider();

    // Assert
    sp.GetRequiredService<ICacheProvider>()
      .Should().BeOfType<MemoryCacheProvider>();
  }
}