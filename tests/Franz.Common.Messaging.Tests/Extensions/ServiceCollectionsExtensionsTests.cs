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

  [Fact]
  public void AddMessagingOptions_WithValidConfiguration_ShouldRegisterOptions()
  {
    // Arrange
    var configDict = new Dictionary<string, string?> { { "Messaging:HostName", "localhost" } };
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

    // Act
    _services.AddMessagingOptions(configuration);

    // Assert
    _services.Should().Contain(sd => sd.ServiceType == typeof(IConfigureOptions<MessagingOptions>));
  }

  [Fact]
  public void AddMessagingOptions_WithMissingConfiguration_ShouldThrowTechnicalException()
  {
    // Arrange
    var configuration = new ConfigurationBuilder().Build(); // Empty config

    // Act
    Action act = () => _services.AddMessagingOptions(configuration);

    // Assert
    act.Should().Throw<TechnicalException>();
  }

  [Fact]
  public void AddMessagingOptions_WhenAlreadyConfigured_ShouldNotOverrideOrThrow()
  {
    // Arrange
    _services.Configure<MessagingOptions>(opt => { });
    var configuration = new ConfigurationBuilder().Build();

    // Act
    Action act = () => _services.AddMessagingOptions(configuration);

    // Assert
    act.Should().NotThrow();
    // Should still only have the one registration we manually added
    _services.Count(sd => sd.ServiceType == typeof(IConfigureOptions<MessagingOptions>)).Should().Be(1);
  }
}