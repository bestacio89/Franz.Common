using System;
using System.Threading;
using System.Threading.Tasks;

using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Mediator.Pipelines.Core;

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
  Mock<ICacheProvider>? cacheMock = null)
  {
    var services = new ServiceCollection();

    var cache = cacheMock ?? new Mock<ICacheProvider>();
    services.AddSingleton<ICacheProvider>(cache.Object);

    services.AddSingleton<ICacheKeyStrategy>(
      Mock.Of<ICacheKeyStrategy>(k =>
        k.BuildKey(It.IsAny<TestRequest>()) == "key"));

    services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

    services.Configure<MediatorCachingOptions>(options =>
    {
      configureOptions?.Invoke(options);
    });

    services.AddFranzMediatorCaching();

    var serviceProvider = services.BuildServiceProvider();

    return serviceProvider
      .GetRequiredService<CachingPipeline<TestRequest, TestResponse>>();
  }

  [Fact]
  public async Task Disabled_Caching_Should_Invoke_Next()
  {
    var pipeline = BuildPipeline(o => o.Enabled = false);

    var result = await pipeline.Handle(
      new TestRequest(1),
      () => Task.FromResult(new TestResponse("computed")));

    result.Value.Should().Be("computed");
  }

  [Fact]
  public async Task Cache_Hit_Should_Return_Cached_Value()
  {
    var cache = new Mock<ICacheProvider>();

    cache.Setup(c => c.ExistsAsync("key", It.IsAny<CancellationToken>()))
         .ReturnsAsync(true);

    cache.Setup(c => c.GetAsync<TestResponse>("key", It.IsAny<CancellationToken>()))
         .ReturnsAsync(new TestResponse("cached"));

    var pipeline = BuildPipeline(null, cache);

    var result = await pipeline.Handle(
      new TestRequest(42),
      () => Task.FromResult(new TestResponse("miss")));

    result.Value.Should().Be("cached");
  }

  [Fact]
  public async Task Cache_Miss_Should_Invoke_Next_And_Set()
  {
    var cache = new Mock<ICacheProvider>();

    cache.Setup(c => c.ExistsAsync("key", It.IsAny<CancellationToken>()))
         .ReturnsAsync(false);

    var pipeline = BuildPipeline(null, cache);

    var result = await pipeline.Handle(
      new TestRequest(99),
      () => Task.FromResult(new TestResponse("computed")));

    result.Value.Should().Be("computed");

    cache.Verify(c =>
      c.SetAsync(
        "key",
        It.IsAny<TestResponse>(),
        It.IsAny<TimeSpan>(),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }
}
