using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Franz.Common.Caching.Tests.Providers;

public sealed class RedisCacheProviderTests : IClassFixture<RedisCacheFixture>
{
  private readonly RedisCacheProvider _provider;
  private readonly IConnectionMultiplexer _muxer;
  private readonly Mock<IOptionsMonitor<CacheOptions>> _optionsMock;
  private readonly CacheOptions _globalOptions;

  public RedisCacheProviderTests(RedisCacheFixture fixture)
  {
    // 🛠️ FIX: Use the multiplexer from the fixture, not a new empty connection
    _muxer = fixture.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

    _globalOptions = new CacheOptions
    {
      KeyPrefix = "test:",
      DefaultAbsoluteExpiration = TimeSpan.FromSeconds(30),
      // Ensure the provider knows where to connect if it needs to re-resolve
      ConnectionString = fixture.ConnectionString
    };

    _optionsMock = new Mock<IOptionsMonitor<CacheOptions>>();
    _optionsMock.Setup(m => m.CurrentValue).Returns(_globalOptions);

    _provider = new RedisCacheProvider(_muxer, _optionsMock.Object);
  }

  [Fact]
  public async Task GetOrSetAsync_Should_Handle_Cache_Miss_And_Hit()
  {
    // Arrange
    var key = Guid.NewGuid().ToString(); // Use unique keys to avoid test pollution
    var expectedValue = "franz-data";
    var calls = 0;

    async Task<string> Factory(CancellationToken ct)
    {
      calls++;
      return await Task.FromResult(expectedValue);
    }

    // Act - First Call (Miss)
    var firstResult = await _provider.GetOrSetAsync(key, Factory);

    // Act - Second Call (Hit)
    var secondResult = await _provider.GetOrSetAsync(key, Factory);

    // Assert
    firstResult.Value.Should().Be(expectedValue);
    firstResult.IsHit.Should().BeFalse();

    secondResult.Value.Should().Be(expectedValue);
    secondResult.IsHit.Should().BeTrue();

    calls.Should().Be(1);
  }

  [Fact]
  public async Task GetOrSetAsync_Should_Respect_Request_Specific_Expiration()
  {
    // Arrange
    var key = Guid.NewGuid().ToString();
    var shortOptions = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromMilliseconds(500)
    };

    // Act
    await _provider.GetOrSetAsync(key, _ => Task.FromResult("val"), shortOptions);

    // Assert hit immediately
    var immediate = await _provider.GetOrSetAsync(key, _ => Task.FromResult("val"));
    immediate.IsHit.Should().BeTrue();

    // Wait for Redis TTL
    await Task.Delay(1000); // 1s to be safe for Redis internal cleanup

    // Assert miss after expiration
    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult("new-val"));
    result.IsHit.Should().BeFalse();
    result.Value.Should().Be("new-val");
  }

  [Fact]
  public async Task RemoveAsync_Should_Delete_Key_With_Prefix()
  {
    // Arrange
    var key = Guid.NewGuid().ToString();
    await _provider.GetOrSetAsync(key, _ => Task.FromResult("data"));

    // Act
    await _provider.RemoveAsync(key);

    // Assert
    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult("fresh"));
    result.IsHit.Should().BeFalse();
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  public async Task GetOrSetAsync_Should_Throw_ArgumentException_On_Invalid_Key(string key)
  {
    var act = () => _provider.GetOrSetAsync(key, _ => Task.FromResult("1"));
    await act.Should().ThrowAsync<ArgumentException>();
  }

  [Fact]
  public async Task RemoveByTagAsync_Should_Throw_NotSupportedException()
  {
    // Act
    var act = () => _provider.RemoveByTagAsync("any-tag");

    // Assert
    await act.Should().ThrowAsync<NotSupportedException>()
        .WithMessage("*performance reasons*");
  }
}