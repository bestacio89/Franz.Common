using FluentAssertions;
using Franz.Common.AzureCosmosDB.Extensions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions; // Assuming your AddFranz... extensions are here
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Caching.Tests.Extensions;

public sealed class AddFranzMediatorCachingTests
{
  private sealed record TestRequest; 
  private sealed record TestResponse;

  [Fact]
  public void Should_Register_CachingPipeline_As_IPipeline_Interface()
  {
    // Arrange
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Franz:Mediator:Caching:DefaultTtl"] = "00:05:00",
          ["Franz:Mediator:Caching:DefaultSlidingExpiration"] = "00:02:00"
        })
        .Build();

    var services = new ServiceCollection();

    // 🔹 ADD THIS: Register IConfiguration so Options binding can find it
    services.AddSingleton<IConfiguration>(configuration);

    services.AddLogging();

    // 1. Memory caching provides the ICacheProvider backend
    services.AddFranzMemoryCaching();

    // 2. Mediator caching registers the pipeline and options
    services.AddFranzMediatorCaching(configuration);

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var pipelines = serviceProvider
        .GetServices<IPipeline<TestRequest, TestResponse>>()
        .ToList();

    // Assert
    pipelines.Should().Contain(p => p.GetType().IsGenericType
                                 && p.GetType().GetGenericTypeDefinition() == typeof(CachingPipeline<,>));
  }

  [Fact]
  public void Should_Register_MediatorCachingOptions_With_Correct_Values()
  {
    // Arrange
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Franz:Mediator:Caching:Enabled"] = "false",
          ["Franz:Mediator:Caching:DefaultTtl"] = "00:10:00"
        })
        .Build();

    var services = new ServiceCollection();

    // 🔹 Register the configuration instance so the Options factory can resolve it
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFranzMediatorCaching(configuration);
    var serviceProvider = services.BuildServiceProvider();

    // Act
    var options = serviceProvider.GetRequiredService<IOptions<MediatorCachingOptions>>().Value;

    // Assert
    options.Enabled.Should().BeFalse();
    options.DefaultTtl.Should().Be(TimeSpan.FromMinutes(10));
  }
}