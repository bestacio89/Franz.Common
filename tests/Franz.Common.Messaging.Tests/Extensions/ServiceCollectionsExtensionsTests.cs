using FluentAssertions;
using Franz.Common.Errors;
using Franz.Common.Headers;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
  private readonly IServiceCollection _services = new ServiceCollection();

  [Fact]
  public void AddMessagingHeaderContext_ShouldRegisterHeaderContextAccessor()
  {
    // Act
    _services.AddMessagingHeaderContext();

    // Assert
    _services.Should().ContainSingle(sd =>
        sd.ServiceType == typeof(IHeaderContextAccessor) &&
        sd.ImplementationType == typeof(HeaderContextAccessor) &&
        sd.Lifetime == ServiceLifetime.Scoped);
  }

  [Fact]
  public void AddMessagingSerialization_ShouldRegisterJsonSerializer()
  {
    // Act
    _services.AddMessagingSerialization();

    // Assert
    _services.Should().ContainSingle(sd =>
        sd.ServiceType == typeof(IMessageSerializer) &&
        sd.ImplementationType == typeof(JsonMessageSerializer) &&
        sd.Lifetime == ServiceLifetime.Scoped);
  }

  [Fact]
  public void AddMessagingFactories_ShouldRegisterFactoryAndAllStrategies()
  {
    // Act
    _services.AddMessagingFactories();

    // Assert
    _services.Should().Contain(sd => sd.ServiceType == typeof(IMessageFactory) && sd.Lifetime == ServiceLifetime.Singleton);

    var strategies = _services.Where(sd => sd.ServiceType == typeof(IMessageBuilderStrategy)).ToList();
    strategies.Should().HaveCount(4);
    strategies.Select(s => s.ImplementationType).Should().Contain(new[]
    {
            typeof(CommandMessageBuilderStrategy),
            typeof(QueryMessageBuilderStrategy),
            typeof(IntegrationEventMessageBuilderStrategy),
            typeof(ExecutionFaultMessageBuilderStrategy)
        });
  }

}