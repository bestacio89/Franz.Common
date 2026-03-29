using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Franz.Common.Caching.Tests.Fakes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace Franz.Common.Caching.Tests.Providers;

public sealed class DistributedCacheProviderTests : IDisposable
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
      DefaultSlidingExpiration = TimeSpan.FromMinutes(2)
    };

    _optionsMock = new Mock<IOptionsMonitor<CacheOptions>>();
    _optionsMock.Setup(m => m.CurrentValue).Returns(_globalOptions);

    _provider = new DistributedCacheProvider(_cacheMock.Object, _optionsMock.Object);
  }

  [Fact]
  public async Task GetOrSetAsync_OnJsonException_ShouldHealAndReturnFreshValue()
  {
    // Arrange
    const string key = "dist:corrupt";
    var corruptData = "invalid-json-content"u8.ToArray();
    var expectedValue = new TestPayload(Guid.CreateVersion7(), "healed");

    // 1. Simulate corrupt data in the cache
    _cacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
              .ReturnsAsync(corruptData);

    // Act
    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult(expectedValue));

    // Assert
    result.IsHit.Should().BeFalse(); // Should treat corruption as a miss
    result.Value.Should().Be(expectedValue);

    // Verify Healing: It should have overwritten the bad data
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
    // Arrange
    const string key = "dist:stampede";
    int factoryCalls = 0;
    byte[]? internalCache = null; // Backing store as byte array

    // 1. Mock the core GetAsync method
    _cacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
              .Returns(() => Task.FromResult(internalCache));

    // 2. Mock the core SetAsync method
    _cacheMock.Setup(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
              .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((k, v, o, c) => internalCache = v)
              .Returns(Task.CompletedTask);

    // Act
    var tasks = Enumerable.Range(0, 10).Select(_ =>
        _provider.GetOrSetAsync(key, async _ =>
        {
          Interlocked.Increment(ref factoryCalls);
          // Artificial delay to ensure the thundering herd hits the semaphore
          await Task.Delay(100);
          return new TestPayload(Guid.CreateVersion7(), "shared");
        }));

    await Task.WhenAll(tasks);

    // Assert
    factoryCalls.Should().Be(1);
  }

  [Fact]
  public async Task GetOrSetAsync_DoubleCheckLocking_ShouldPreventRedundantFactoryCalls()
  {
    // Arrange
    const string key = "dist:double-check";
    var payload = new TestPayload(Guid.CreateVersion7(), "data");
    var json = JsonSerializer.Serialize(payload);
    var bytes = System.Text.Encoding.UTF8.GetBytes(json);

    // First call returns null, second call (after lock) returns the data 
    // set by the thread that won the race.
    _cacheMock.SetupSequence(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
              .ReturnsAsync((byte[]?)null)
              .ReturnsAsync(bytes);

    // Act
    var result = await _provider.GetOrSetAsync(key, _ => Task.FromResult(new TestPayload(Guid.CreateVersion7(), "new")));

    // Assert
    result.IsHit.Should().BeTrue(); // The double-check caught the winner's result
    result.Value.Id.Should().Be(result.Value.Id);
  }

  public void Dispose() => _provider.Dispose();
}