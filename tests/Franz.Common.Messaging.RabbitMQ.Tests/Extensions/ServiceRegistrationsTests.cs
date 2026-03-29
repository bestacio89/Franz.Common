#nullable enable
using FluentAssertions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Infrastructure;

[Collection(nameof(RabbitMqTestCollection))]
public sealed class ServiceRegistrationTests
{
  private readonly IServiceCollection _services = new ServiceCollection();
  private readonly IConfiguration _configuration;
  private readonly RabbitMqContainerFixture _fixture;

  public ServiceRegistrationTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;

    _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:RabbitMQ:BootStrapServers"] = _fixture.ConnectionString,
          ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = "5"
        })
        .Build();
  }

  [Fact]
  public async Task AddRabbitMQMessaging_ShouldProvideFunctionalConnectionProvider()
  {
    // Arrange
    _services.AddRabbitMQMessaging(_configuration);
    var provider = _services.BuildServiceProvider();

    // Assert DI registration
    _services.Should().ContainSingle(s =>
        s.ServiceType == typeof(IConnectionProvider) &&
        s.Lifetime == ServiceLifetime.Singleton);

    // Act
    var connectionProvider = provider.GetRequiredService<IConnectionProvider>();
    using var connection = await connectionProvider.GetConnectionAsync();

    // Assert connection
    connection.Should().NotBeNull();
    connection.IsOpen.Should().BeTrue();
  }

  [Fact]
  public void AddRabbitMQMessaging_ShouldRegisterCoreInfrastructureAsSingletons()
  {
    _services.AddRabbitMQMessaging(_configuration);

    _services.Should().ContainSingle(s =>
        s.ServiceType == typeof(IConnectionProvider) &&
        s.Lifetime == ServiceLifetime.Singleton);

    _services.Should().ContainSingle(s =>
        s.ServiceType == typeof(IChannelPool) &&
        s.Lifetime == ServiceLifetime.Singleton);

    _services.Should().ContainSingle(s =>
        s.ServiceType == typeof(IModelProvider) &&
        s.Lifetime == ServiceLifetime.Singleton);
  }

  [Fact]
  public async Task AddRabbitMQMessaging_ShouldRegisterPublisherAndTransactionAsScoped()
  {
    // Arrange
    _services.AddRabbitMQMessaging(_configuration);
    var provider = _services.BuildServiceProvider();

    // Act & Assert
    // FIX: Use 'await using' to allow the container to call DisposeAsync()
    await using (var scope = provider.CreateAsyncScope())
    {
      var publisher = scope.ServiceProvider.GetRequiredService<IMessagingPublisher>();
      publisher.Should().BeOfType<RabbitMQMessagingPublisher>();

      var transaction = scope.ServiceProvider.GetRequiredService<IMessagingTransaction>();
      transaction.Should().NotBeNull();
    } // Async disposal happens here automatically
  }

  [Fact]
  public void AddRabbitMQMessaging_ShouldConfigureOptionsFromContainerFixture()
  {
    _services.AddRabbitMQMessaging(_configuration);
    var provider = _services.BuildServiceProvider();

    var options = provider.GetRequiredService<IOptions<RabbitMQMessagingOptions>>().Value;
    options.BootStrapServers.Should().Be(_fixture.ConnectionString);
  }

  [Fact]
  public void AddRabbitMQMessaging_ShouldBeIdempotent()
  {
    _services.AddRabbitMQMessaging(_configuration);
    _services.AddRabbitMQMessaging(_configuration); // called twice

    // Check idempotent registrations
    _services.Count(s => s.ServiceType == typeof(IMessagingPublisher)).Should().Be(1);
    _services.Count(s => s.ServiceType == typeof(IChannelPool)).Should().Be(1);
    _services.Count(s => s.ServiceType == typeof(IConnectionProvider)).Should().Be(1);
    _services.Count(s => s.ServiceType == typeof(IModelProvider)).Should().Be(1);
  }
}