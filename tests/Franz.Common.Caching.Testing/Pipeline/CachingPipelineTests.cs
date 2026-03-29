using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Caching.Tests.Pipelines;

public sealed class CachingPipelineTests
{
  public sealed record TestRequest(int Id);
  public sealed record TestResponse(string Value);

  /// <summary>
  /// Build the pipeline with specific options and cache provider.
  /// Uses Options.Create to bypass 'init' property restrictions in tests.
  /// </summary>
  private CachingPipeline<TestRequest, TestResponse> BuildPipeline(
    MediatorCachingOptions? options = null,
    ICacheProvider? cacheProvider = null)
  {
    // Use object initializer to satisfy 'required' members in CacheOptions/MediatorCachingOptions
    var effectiveOptions = options ?? new MediatorCachingOptions
    {
      Enabled = true,
      // Assuming MediatorCachingOptions inherits or contains CacheOptions
   
      DefaultSlidingExpiration = TimeSpan.FromMinutes(20),
      LogHitLevel = LogLevel.Information,
      LogMissLevel = LogLevel.Information
    };

    var optionsMonitorMock = new Mock<IOptionsMonitor<MediatorCachingOptions>>();
    optionsMonitorMock.Setup(m => m.CurrentValue).Returns(effectiveOptions);

    var keyStrategyMock = new Mock<ICacheKeyStrategy>();
    keyStrategyMock.Setup(s => s.BuildKey(It.IsAny<object>())).Returns("key");

    var loggerMock = new Mock<ILogger<CachingPipeline<TestRequest, TestResponse>>>();
    var metadataProviders = Enumerable.Empty<ICacheMetadataProvider>();

    return new CachingPipeline<TestRequest, TestResponse>(
        cacheProvider ?? new Mock<ICacheProvider>().Object,
        optionsMonitorMock.Object,
        keyStrategyMock.Object,
        loggerMock.Object,
        metadataProviders
    );
  }

  [Fact]
  public async Task Handle_WhenDisabled_ShouldInvokeNextAndSkipCache()
  {
    // Arrange
    var mockCache = new Mock<ICacheProvider>();
    var options = new MediatorCachingOptions { Enabled = false };
    var pipeline = BuildPipeline(options, mockCache.Object);
    var nextCalled = false;

    // Act
    var result = await pipeline.Handle(
        new TestRequest(1),
        () => {
          nextCalled = true;
          return Task.FromResult(new TestResponse("computed"));
        });

    // Assert
    result.Value.Should().Be("computed");
    nextCalled.Should().BeTrue();

    mockCache.Verify(c => c.GetOrSetAsync<TestResponse>(
        It.IsAny<string>(),
        It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
        It.IsAny<CacheOptions>(),
        It.IsAny<CancellationToken>()),
        Times.Never);
  }

  [Fact]
  public async Task Handle_OnCacheHit_ShouldReturnCachedValueWithoutInvokingNext()
  {
    // Arrange
    var cacheMock = new Mock<ICacheProvider>();
    var cachedResponse = new TestResponse("cached-value");
    var nextCalled = false;

    // Direct Return for a Cache Hit
    cacheMock.Setup(c => c.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
            It.IsAny<CacheOptions>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new CacheResult<TestResponse>(cachedResponse, IsHit: true));

    var pipeline = BuildPipeline(new MediatorCachingOptions { Enabled = true }, cacheMock.Object);

    // Act
    var result = await pipeline.Handle(
        new TestRequest(42),
        () => {
          nextCalled = true;
          return Task.FromResult(new TestResponse("should-not-run"));
        },
        CancellationToken.None);

    // Assert
    result.Value.Should().Be("cached-value");
    nextCalled.Should().BeFalse(); // Execution is short-circuited
  }

  [Fact]
  public async Task Handle_OnCacheMiss_ShouldInvokeNextAndReturnComputedValue()
  {
    // Arrange
    var cacheMock = new Mock<ICacheProvider>();
    var computedResponse = new TestResponse("computed-value");

    // Simulate a cache miss: Execute the factory and return result with IsHit = false
    cacheMock.Setup(c => c.GetOrSetAsync(
            "key",
            It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
            It.IsAny<CacheOptions>(),
            It.IsAny<CancellationToken>()))
        .Returns(async (string k, Func<CancellationToken, Task<TestResponse>> factory, CacheOptions? opts, CancellationToken ct) =>
        {
          var value = await factory(ct);
          return new CacheResult<TestResponse>(value, IsHit: false);
        });

    var pipeline = BuildPipeline(null, cacheMock.Object);

    // Act
    var result = await pipeline.Handle(
        new TestRequest(99),
        () => Task.FromResult(computedResponse)
    );

    // Assert
    result.Value.Should().Be("computed-value");

    cacheMock.Verify(c => c.GetOrSetAsync(
        "key",
        It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
        It.IsAny<CacheOptions>(),
        It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task GetOrSetAsync_OnCorruptJson_ShouldHealByInvokingFactory()
  {
    // Arrange
    var key = "corrupt-key";
    var expectedValue = new TestResponse("healed-data");
    var corruptBytes = System.Text.Encoding.UTF8.GetBytes("{ invalid json }");

    var cacheMock = new Mock<IDistributedCache>();
    cacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
        .ReturnsAsync(corruptBytes);

    // satisfy 'required' members
    var options = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5),
      DefaultSlidingExpiration = TimeSpan.FromMinutes(1)
    };

    var optionsMonitorMock = new Mock<IOptionsMonitor<CacheOptions>>();
    optionsMonitorMock.Setup(m => m.CurrentValue).Returns(options);

    var provider = new DistributedCacheProvider(cacheMock.Object, optionsMonitorMock.Object);
    bool factoryWasCalled = false;

    // Act
    var result = await provider.GetOrSetAsync(
        key,
        async ct =>
        {
          factoryWasCalled = true;
          return await Task.FromResult(expectedValue);
        },
        ct: CancellationToken.None);

    // Assert
    result.IsHit.Should().BeFalse(); // Corrupt data is a miss
    result.Value.Should().BeEquivalentTo(expectedValue);
    factoryWasCalled.Should().BeTrue();

    // Verify 'Healing' - should overwrite the corrupt entry
    cacheMock.Verify(c => c.SetAsync(
        key,
        It.IsAny<byte[]>(),
        It.IsAny<DistributedCacheEntryOptions>(),
        It.IsAny<CancellationToken>()),
        Times.Once);
  }
}