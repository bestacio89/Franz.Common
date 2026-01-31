using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Franz.Common.Caching.Testing.Pipelines;

public sealed class CachingPipelineTests
{
  private sealed record TestRequest(int Id);
  private sealed record TestResponse(string Value);

  private static CachingPipeline<TestRequest, TestResponse> BuildPipeline(
      Action<MediatorCachingOptions>? configureOptions = null,
      ICacheProvider? cacheProvider = null)
  {
    var services = new ServiceCollection();

    // Use a mock cache if none provided
    cacheProvider ??= new Mock<ICacheProvider>().Object;
    services.AddSingleton(cacheProvider);

    // Simple cache key strategy mock
    services.AddSingleton(Mock.Of<ICacheKeyStrategy>(k =>
        k.BuildKey(It.IsAny<TestRequest>()) == "key"));

    // Null logger
    services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

    // Always configure options to ensure IOptions<MediatorCachingOptions> is registered
    services.Configure(configureOptions ?? (_ => { }));

    // Register the pipeline
    services.AddTransient<CachingPipeline<TestRequest, TestResponse>>();

    return services.BuildServiceProvider()
                   .GetRequiredService<CachingPipeline<TestRequest, TestResponse>>();
  }

  [Fact]
  public async Task Disabled_Caching_Should_Invoke_Next()
  {
    var mockCache = new Mock<ICacheProvider>();
    var pipeline = BuildPipeline(o => o.Enabled = false, mockCache.Object);

    var result = await pipeline.Handle(
        new TestRequest(1),
        () => Task.FromResult(new TestResponse("computed")));

    result.Value.Should().Be("computed");

    // Cache should never be called
    mockCache.Verify(c => c.GetOrSetAsync<TestResponse>(
        It.IsAny<string>(),
        It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
        It.IsAny<CacheOptions>(),
        It.IsAny<CancellationToken>()),
        Times.Never);
  }

  [Fact]
  public async Task Cache_Hit_Should_Return_Cached_Value()
  {
    var cacheMock = new Mock<ICacheProvider>();

    cacheMock.Setup(c => c.GetOrSetAsync(
            "key",
            It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
            It.IsAny<CacheOptions>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new TestResponse("cached"));

    var pipeline = BuildPipeline(null, cacheMock.Object);

    var result = await pipeline.Handle(
        new TestRequest(42),
        () => Task.FromResult(new TestResponse("miss")));

    result.Value.Should().Be("cached");

    // Verify factory was never invoked because cache hit
    cacheMock.Verify(c => c.GetOrSetAsync(
        "key",
        It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
        It.IsAny<CacheOptions>(),
        It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task Cache_Miss_Should_Invoke_Next_And_Set()
  {
    var cacheMock = new Mock<ICacheProvider>();

    // Setup GetOrSetAsync to actually call the factory (simulate cache miss)
    cacheMock.Setup(c => c.GetOrSetAsync(
            "key",
            It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
            It.IsAny<CacheOptions>(),
            It.IsAny<CancellationToken>()))
        .Returns((string k, Func<CancellationToken, Task<TestResponse>> factory, CacheOptions opts, CancellationToken ct) =>
            factory(ct));

    var pipeline = BuildPipeline(null, cacheMock.Object);

    var result = await pipeline.Handle(
        new TestRequest(99),
        () => Task.FromResult(new TestResponse("computed")));

    result.Value.Should().Be("computed");

    // Ensure GetOrSetAsync was called
    cacheMock.Verify(c => c.GetOrSetAsync(
        "key",
        It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
        It.IsAny<CacheOptions>(),
        It.IsAny<CancellationToken>()),
        Times.Once);
  }
}
