#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Tests.Infrastructure;
using Franz.Common.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Security;
using RabbitMQ.Client;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Connections;

[Collection(nameof(RabbitMqTestCollection))]
public sealed class ConnectionTests
{
  private readonly RabbitMqContainerFixture _fixture;

  public ConnectionTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;
  }

  private IServiceCollection CreateBaseServiceCollection()
  {
    var services = new ServiceCollection();
    // Senior Note: Always use BootStrapServers for URI-based connection in v7+
    services.AddSingleton(Options.Create(new RabbitMQMessagingOptions
    {
      BootStrapServers = _fixture.ConnectionString,
      UserName = "guest",
      Password = "guest"
    }));

    services.AddSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>();
    services.AddSingleton<IConnectionProvider, ConnectionProvider>();
    return services;
  }

  [Fact]
  public void ConnectionFactoryProvider_ShouldRespectRecoveryAndHeartbeatOptions()
  {
    var options = Options.Create(new RabbitMQMessagingOptions
    {
      BootStrapServers = _fixture.ConnectionString,
      AutomaticRecoveryEnabled = false,
      TopologyRecoveryEnabled = false,
      RequestedHeartbeatSeconds = 17
    });
    var provider = new ConnectionFactoryProvider(options, new AssemblyAccessorWrapper());

    var factory = (ConnectionFactory)provider.Current;

    factory.AutomaticRecoveryEnabled.Should().BeFalse();
    factory.TopologyRecoveryEnabled.Should().BeFalse();
    factory.RequestedHeartbeat.Should().Be(TimeSpan.FromSeconds(17));
  }

  [Fact]
  public void ConnectionFactoryProvider_ShouldUseStrictTlsValidation()
  {
    var options = Options.Create(new RabbitMQMessagingOptions
    {
      HostName = "rabbit.example.test",
      SslEnabled = true
    });
    var provider = new ConnectionFactoryProvider(options, new AssemblyAccessorWrapper());

    var factory = (ConnectionFactory)provider.Current;

    factory.Ssl.Enabled.Should().BeTrue();
    factory.Ssl.AcceptablePolicyErrors.Should().Be(SslPolicyErrors.None);
    factory.Port.Should().Be(5671);
  }

  [Fact]
  public void ConnectionFactoryProvider_ShouldRejectMalformedBootstrapUri()
  {
    var options = Options.Create(new RabbitMQMessagingOptions
    {
      BootStrapServers = "not-a-valid-rabbitmq-uri"
    });
    var provider = new ConnectionFactoryProvider(options, new AssemblyAccessorWrapper());

    Action act = () => _ = provider.Current;

    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*BootStrapServers*");
  }

  [Fact]
  public async Task ConnectionProvider_Should_Return_Open_Connection()
  {
    // Arrange
    var services = CreateBaseServiceCollection();
    services.AddSingleton<IAssemblyAccessor, AssemblyAccessorWrapper>();
    var serviceProvider = services.BuildServiceProvider();
    var connectionProvider = serviceProvider.GetRequiredService<IConnectionProvider>();

    // Act
    var connection = await connectionProvider.GetConnectionAsync();

    // Assert
    connection.Should().NotBeNull();
    connection.IsOpen.Should().BeTrue();
  }

  [Fact]
  public async Task GetConnectionAsync_ShouldBeThreadSafe()
  {
    // Arrange
    var services = CreateBaseServiceCollection();
    services.AddSingleton<IAssemblyAccessor, AssemblyAccessorWrapper>();
    var serviceProvider = services.BuildServiceProvider();
    var connectionProvider = serviceProvider.GetRequiredService<IConnectionProvider>();

    // Act
    // We execute 20 simultaneous requests to ensure SemaphoreSlim(1,1) is working
    var parallelTasks = Enumerable.Range(0, 20)
        .Select(_ => connectionProvider.GetConnectionAsync().AsTask())
        .ToArray();

    var connections = await Task.WhenAll(parallelTasks);

    // Assert
    // All tasks must return the exact same reference (Singleton behavior via Double-Check Locking)
    connections.Should().OnlyContain(c => ReferenceEquals(c, connections[0]));
    connections[0].IsOpen.Should().BeTrue();
  }

  [Fact]
  public async Task GetModelAsync_ShouldReturnOpenChannel()
  {
    // Arrange
    var services = CreateBaseServiceCollection();
    services.AddSingleton<IAssemblyAccessor, AssemblyAccessorWrapper>();
    services.AddSingleton<IModelProvider, ModelProvider>();
    var serviceProvider = services.BuildServiceProvider();

    var modelProvider = serviceProvider.GetRequiredService<IModelProvider>();

    // Act
    var model = await modelProvider.GetChannelAsync();

    // Assert
    model.Should().NotBeNull();
    model.IsOpen.Should().BeTrue();
  }
}