#nullable enable
using DotNet.Testcontainers.Builders;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using Franz.Common.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Infrastructure;

/// <summary>
/// xUnit Collection definition for RabbitMQ integration tests
/// </summary>
[CollectionDefinition(nameof(RabbitMqTestCollection))]
public sealed class RabbitMqTestCollection : ICollectionFixture<RabbitMqContainerFixture>
{
  // Marker class for xUnit collection
}

/// <summary>
/// Shared RabbitMQ container fixture with DI configured for ConnectionFactoryProvider & ConnectionProvider
/// </summary>
public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
  public const string Image = "rabbitmq:4.0-management";

  public RabbitMqContainer Container { get; } = new RabbitMqBuilder(Image)
      .WithCleanUp(true)
      .WithWaitStrategy(Wait.ForUnixContainer()
          .UntilInternalTcpPortIsAvailable(5672))
      .WithStartupTimeout(TimeSpan.FromMinutes(2))
      .Build();

  public string ConnectionString { get; private set; } = default!;
  public ServiceProvider ServiceProvider { get; private set; } = default!;

  public Task InitializeAsync() => InitializeAsyncInternal();

  private async Task InitializeAsyncInternal()
  {
    await Container.StartAsync();
    ConnectionString = Container.GetConnectionString();

    var services = new ServiceCollection();
    services.AddSingleton(Options.Create(new RabbitMQMessagingOptions
    {
      BootStrapServers = ConnectionString,
      UserName = "guest",
      Password = "guest",
      PublisherConfirmTimeoutSeconds = 5
    }));

    services.AddSingleton<IAssemblyAccessor, AssemblyAccessorWrapper>();
    services.AddSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>();
    services.AddSingleton<IConnectionProvider, ConnectionProvider>();
    services.AddSingleton<IChannelPool, ChannelPool>();
    services.AddScoped<IMessagingInitializer, RabbitMQMessagingInitializer>();
    services.AddScoped<IMessagingTransaction, RabbitMQMessagingTransaction>();

    ServiceProvider = services.BuildServiceProvider();
  }

  public async Task DisposeAsync()
  {
    if (ServiceProvider is not null)
    {
      await ServiceProvider.DisposeAsync();
      ServiceProvider = null!;
    }

    await Container.DisposeAsync().ConfigureAwait(false);
  }
}
