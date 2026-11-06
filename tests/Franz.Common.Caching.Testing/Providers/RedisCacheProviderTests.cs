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
    _muxer = fixture.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

    _globalOptions = new CacheOptions
    {
      KeyPrefix = "test:",
      DefaultAbsoluteExpiration = TimeSpan.FromSeconds(30),
      DefaultSlidingExpiration = TimeSpan.FromMinutes(20),
      ConnectionString = fixture.ConnectionString
    };

    _optionsMock = new Mock<IOptionsMonitor<CacheOptions>>();
    _optionsMock.Setup(m => m.CurrentValue).Returns(_globalOptions);

    _provider = new RedisCacheProvider(
        _muxer,
        _optionsMock.Object,
        Microsoft.Extensions.Options.Options.Create(TestJsonSerializerOptions.Create()));
  }

  [Fact]
  public async Task GetOrSetAsync_Should_Handle_Cache_Miss_And_Hit()
  {
    var key = Guid.NewGuid().ToString();
    const string expectedValue = "franz-data";
    var calls = 0;

    Task<string> Factory(CancellationToken ct)
    {
      calls++;
      return Task.FromResult(expectedValue);
    }

    var firstResult = await _provider.GetOrSetAsync(key, Factory);
    var secondResult = await _provider.GetOrSetAsync(key, Factory);

    firstResult.Value.Should().Be(expectedValue);
    firstResult.IsHit.Should().BeFalse();
    secondResult.Value.Should().Be(expectedValue);
    secondResult.IsHit.Should().BeTrue();
    calls.Should().Be(1);
  }

  [Fact]
  public async Task GetOrSetAsync_Should_Respect_Request_Specific_Expiration()
  {
    var key = Guid.NewGuid().ToString();
    var shortOptions = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromMilliseconds(500)
    };

    await _provider.GetOrSetAsync(key, _ => Task.FromResult("val"), shortOptions);

    var immediate = await _provider.GetOrSetAsync(key, _ => Task.FromResult("val"));
    immediate.IsHit.Should().BeTrue();

    await Task.Delay(1000);

    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult("new-val"));
    result.IsHit.Should().BeFalse();
    result.Value.Should().Be("new-val");
  }

  [Fact]
  public async Task RemoveAsync_Should_Delete_Key_With_Prefix()
  {
    var key = Guid.NewGuid().ToString();
    await _provider.GetOrSetAsync(key, _ => Task.FromResult("data"));
    await _provider.RemoveAsync(key);

    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult("fresh"));
    result.IsHit.Should().BeFalse();
  }

  [Fact]
  public async Task SetAsync_Should_Preserve_Null_As_A_Cached_Value()
  {
    var key = Guid.NewGuid().ToString();
    await _provider.SetAsync<string?>(key, null);

    var factoryCalls = 0;
    var result = await _provider.GetOrSetAsync<string?>(key, _ =>
    {
      factoryCalls++;
      return Task.FromResult<string?>("factory");
    });

    result.IsHit.Should().BeTrue();
    result.Value.Should().BeNull();
    factoryCalls.Should().Be(0);
  }

  [Fact]
  public async Task SlidingExpiration_Should_Refresh_Until_AbsoluteExpiration()
  {
    var key = Guid.NewGuid().ToString();
    var options = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromSeconds(4),
      DefaultSlidingExpiration = TimeSpan.FromSeconds(2)
    };

    var first = await _provider.GetOrSetAsync(key, _ => Task.FromResult("cached"), options);
    first.IsHit.Should().BeFalse();

    var differentReadOptions = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromSeconds(30),
      DefaultSlidingExpiration = TimeSpan.FromMilliseconds(100)
    };

    for (var access = 0; access < 6; access++)
    {
      await Task.Delay(500);
      var hit = await _provider.GetOrSetAsync(key, _ => Task.FromResult("replacement"), differentReadOptions);
      hit.IsHit.Should().BeTrue();
    }

    await Task.Delay(1200);
    var expired = await _provider.GetOrSetAsync(key, _ => Task.FromResult("fresh"), options);
    expired.IsHit.Should().BeFalse();
    expired.Value.Should().Be("fresh");
  }

  [Fact]
  public async Task NoExpiration_Should_Not_InheritSlidingExpirationFromALaterRead()
  {
    var key = Guid.NewGuid().ToString();
    _globalOptions.DefaultSlidingExpiration = TimeSpan.FromMilliseconds(200);

    var noExpiration = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.Zero,
      DefaultSlidingExpiration = TimeSpan.Zero
    };

    await _provider.SetAsync(key, "persistent", noExpiration);

    await Task.Delay(50);
    var firstRead = await _provider.GetOrSetAsync(key, _ => Task.FromResult("replacement"));
    firstRead.IsHit.Should().BeTrue();
    firstRead.Value.Should().Be("persistent");

    await Task.Delay(350);
    var secondRead = await _provider.GetOrSetAsync(key, _ => Task.FromResult("replacement"));
    secondRead.IsHit.Should().BeTrue();
    secondRead.Value.Should().Be("persistent");
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  public async Task GetOrSetAsync_Should_Throw_ArgumentException_On_Invalid_Key(string key)
  {
    var act = () => _provider.GetOrSetAsync(key, _ => Task.FromResult("1"));
    await act.Should().ThrowAsync<ArgumentException>();
  }
}
