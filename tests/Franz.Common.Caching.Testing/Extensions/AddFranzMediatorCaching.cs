using FluentAssertions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Caching.Testing.Models;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Caching.Testing.Extensions;

public sealed class AddFranzMediatorCachingTests
{
  private sealed record TestRequest;
  private sealed record TestResponse;

  [Fact]
  public void Should_Register_CachingPipeline()
  {
    using var serviceProvider = ServiceTestHelper.Build(services =>
    {
      services.AddLogging();
      services.AddFranzMemoryCaching();     // provides ICacheProvider & friends
      services.AddFranzMediatorCaching();   // registers caching pipeline
    });

    var pipelines = serviceProvider
      .GetServices<IPipeline<TestRequest, TestResponse>>()
      .ToList();

    pipelines.Should().ContainSingle();
    pipelines.Single().Should()
      .BeOfType<CachingPipeline<TestRequest, TestResponse>>();
  }
}
