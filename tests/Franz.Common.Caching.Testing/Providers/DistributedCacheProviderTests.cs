using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Franz.Common.Caching.Tests.Fakes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace Franz.Common.Caching.Tests.Providers;

public sealed class DistributedCacheProviderTests
{
  private readonly Mock<IDistributedCache> _cacheMock;
  private readonly Mock<IOptionsMonitor<CacheOptions>> _optionsMock;
  private readonly DistributedCacheProvider _provider;
  private readonly CacheOptions _globalOptions;

  public DistributedCacheProviderTests()
  {
    _cacheMock = new Mock<IDistributedCache>();
    _globalOptions = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromMinutes(10),
      DefaultSlidingExpiration = TimeSpan.FromMinutes(2),
      KeyPrefix = string.Empty
    };

    _optionsMock = new Mock<IOptionsMonitor<CacheOptions>>();
    _optionsMock.Setup(m => m.CurrentValue).Returns(_globalOptions);

    _provider = new DistributedCacheProvider(_cacheMock.Object, _optionsMock.Object);
  }

  [Fact]
  public async Task GetOrSetAsync_OnJsonException_ShouldHealAndReturnFreshValue()
  {
    const string key = "dist:corrupt";
    var corruptData = "invalid-json-content"u8.ToArray();
    var expectedValue = new TestPayload(Guid.CreateVersion7(), "healed");

    _cacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
              .ReturnsAsync(corruptData);

    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult(expectedValue));

    result.IsHit.Should().BeFalse();
    result.Value.Should().Be(expectedValue);

    _cacheMock.Verify(c => c.SetAsync(
        key,
        It.IsAny<byte[]>(),
        It.IsAny<DistributedCacheEntryOptions>(),
        It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task GetOrSetAsync_ConcurrentRequests_ShouldInvokeFactoryOnce()
  {
    const string key = "dist:stampede";
    int factoryCalls = 0;
    byte[]? internalCache = null;

    _cacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
              .Returns(() => Task.FromResult(internalCache));

    _cacheMock.Setup(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
              .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((k, v, o, c) => internalCache = v)
              .Returns(Task.CompletedTask);

    var tasks = Enumerable.Range(0, 10).Select(_ =>
        _provider.GetOrSetAsync(key, async _ =>
        {
          Interlocked.Increment(ref factoryCalls);
          await Task.Delay(100);
          return new TestPayload(Guid.CreateVersion7(), "shared");
        }));

    await Task.WhenAll(tasks);
    factoryCalls.Should().Be(1);
  }

  [Fact]
  public async Task GetOrSetAsync_DoubleCheckLocking_ShouldPreventRedundantFactoryCalls()
  {
    const string key = "dist:double-check";
    var payload = new TestPayload(Guid.CreateVersion7(), "data");
    var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

    _cacheMock.SetupSequence(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
              .ReturnsAsync((byte[]?)null)
              .ReturnsAsync(bytes);

    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult(new TestPayload(Guid.CreateVersion7(), "new")));

    result.IsHit.Should().BeTrue();
    result.Value.Should().Be(payload);
  }

  [Fact]
  public async Task GetOrSetAsync_AndRemoveAsync_ShouldApplyConfiguredKeyPrefix()
  {
    _globalOptions.KeyPrefix = "franz:";
    const string key = "dist:prefixed";
    const string fullKey = "franz:dist:prefixed";
    var payload = new TestPayload(Guid.CreateVersion7(), "cached");
    var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

    _cacheMock.Setup(cache => cache.GetAsync(fullKey, It.IsAny<CancellationToken>()))
      .ReturnsAsync(bytes);

    var result = await _provider.GetOrSetAsync<TestPayload>(key, _ => throw new InvalidOperationException("Factory must not run."));

    result.IsHit.Should().BeTrue();
    result.Value.Should().Be(payload);

    await _provider.RemoveAsync(key);
    _cacheMock.Verify(cache => cache.RemoveAsync(fullKey, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task GetOrSetAsync_ShouldTreatJsonNullAsAHit()
  {
    const string key = "dist:null";
    _cacheMock.Setup(cache => cache.GetStringAsync(key, It.IsAny<CancellationToken>()))
      .ReturnsAsync("null");

    var factoryCalls = 0;
    var result = await _provider.GetOrSetAsync<string?>(key, _ =>
    {
      factoryCalls++;
      return Task.FromResult<string?>(null);
    });

    result.IsHit.Should().BeTrue();
    result.Value.Should().BeNull();
    factoryCalls.Should().Be(0);
  }

  [Fact]
  public async Task SetAsync_ShouldReplaceAnExistingEntry()
  {
    const string key = "settings:theme";
    var stored = new Dictionary<string, byte[]>();
    _cacheMock.Setup(cache => cache.GetAsync(key, It.IsAny<CancellationToken>()))
      .Returns((string _, CancellationToken _) => Task.FromResult(stored.TryGetValue(key, out var value) ? value : null));
    _cacheMock.Setup(cache => cache.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
      .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, value, _, _) => stored[key] = value)
      .Returns(Task.CompletedTask);

    await _provider.SetAsync(key, "new-value");
    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult("factory-value"));

    result.IsHit.Should().BeTrue();
    result.Value.Should().Be("new-value");
  }
}
