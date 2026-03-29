using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Caching.Tests.Providers;

public sealed class MemoryCacheProviderTests : IDisposable
{
  private readonly IMemoryCache _memoryCache;
  private readonly Mock<IOptionsMonitor<CacheOptions>> _optionsMock;
  private readonly CacheOptions _globalOptions;
  private readonly MemoryCacheProvider _provider;

  public MemoryCacheProviderTests()
  {
    _memoryCache = new MemoryCache(new MemoryCacheOptions());

    _globalOptions = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5)
    };

    _optionsMock = new Mock<IOptionsMonitor<CacheOptions>>();
    _optionsMock.Setup(m => m.CurrentValue).Returns(_globalOptions);

    _provider = new MemoryCacheProvider(_memoryCache, _optionsMock.Object);
  }

  [Fact]
  public async Task GetOrSetAsync_Should_Return_Value_And_Manage_Cache_State()
  {
    // Arrange
    const string key = "mem:test:1";
    const string value = "franz-memory-data";
    var calls = 0;

    Func<CancellationToken, Task<string>> factory = _ =>
    {
      calls++;
      return Task.FromResult(value);
    };

    // Act - Initial Load (Miss)
    var firstResult = await _provider.GetOrSetAsync(key, factory);

    // Act - Subsequent Load (Hit)
    var secondResult = await _provider.GetOrSetAsync(key, factory);

    // Assert
    firstResult.Value.Should().Be(value);
    firstResult.IsHit.Should().BeFalse();

    secondResult.Value.Should().Be(value);
    secondResult.IsHit.Should().BeTrue();

    calls.Should().Be(1);
  }

  [Fact]
  public async Task RemoveAsync_Should_Invalidate_Key()
  {
    // Arrange
    const string key = "mem:test:remove";
    await _provider.GetOrSetAsync(key, _ => Task.FromResult(100));

    // Act
    await _provider.RemoveAsync(key);

    // Assert
    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult(200));
    result.IsHit.Should().BeFalse();
    result.Value.Should().Be(200);
  }

  [Fact]
  public async Task GetOrSetAsync_Should_Respect_Request_Specific_Expiration()
  {
    // Arrange
    const string key = "mem:test:ttl";
    var requestOptions = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromMilliseconds(200)
    };

    // Act
    await _provider.GetOrSetAsync(key, _ => Task.FromResult("expiring"), requestOptions);

    // Wait for memory cache eviction
    await Task.Delay(300);

    // Assert
    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult("fresh"));
    result.IsHit.Should().BeFalse();
    result.Value.Should().Be("fresh");
  }

  [Fact]
  public async Task RemoveByTagAsync_Should_Throw_NotSupportedException()
  {
    // Arrange & Act
    var act = () => _provider.RemoveByTagAsync("any-tag");

    // Assert
    await act.Should().ThrowAsync<NotSupportedException>()
        .WithMessage("*Tag-based*support*"); // Broadened to catch 'support' or 'supported'
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task GetOrSetAsync_Should_Throw_ArgumentException_On_Invalid_Key(string invalidKey)
  {
    // Act
    var act = () => _provider.GetOrSetAsync(invalidKey, _ => Task.FromResult(1));

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  public void Dispose()
  {
    _memoryCache.Dispose();
  }
}