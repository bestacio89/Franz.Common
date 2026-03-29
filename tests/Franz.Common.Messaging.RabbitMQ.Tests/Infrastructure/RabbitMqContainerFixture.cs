#nullable enable
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
  // --- Testcontainers RabbitMQ ---
  public RabbitMqContainer Container { get; } = new RabbitMqBuilder("rabbitmq:4.0-management")
      .WithCleanUp(true)
      .Build();

  // --- Connection string dynamically provided by Testcontainer ---
  public string ConnectionString { get; private set; } = default!;

  // --- Service provider for DI ---
  public ServiceProvider ServiceProvider { get; private set; } = default!;

  // --- Initialize container + DI ---
  public Task InitializeAsync() => InitializeAsyncInternal();

  private async Task InitializeAsyncInternal()
  {
    // Start RabbitMQ container
    await Container.StartAsync();

    // Get dynamic connection string (amqp://guest:guest@localhost:port/)
    ConnectionString = Container.GetConnectionString();

    // Build DI container
    var services = new ServiceCollection();

    // Register strongly typed RabbitMQMessagingOptions
    services.AddSingleton(Options.Create(new RabbitMQMessagingOptions
    {
      BootStrapServers = ConnectionString,
      UserName = "guest",
      Password = "guest",
      PublisherConfirmTimeoutSeconds = 5
    }));

    // Register AssemblyAccessor
    services.AddSingleton<IAssemblyAccessor, AssemblyAccessorWrapper>();
  
    // Register factories and providers exactly as in production
    services.AddSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>();
    services.AddSingleton<IConnectionProvider, ConnectionProvider>();

    // Register channel pool if used in your tests
    services.AddSingleton<IChannelPool, ChannelPool>();

    // Register messaging initializers & transactions
    services.AddScoped<IMessagingInitializer, RabbitMQMessagingInitializer>();
    services.AddScoped<IMessagingTransaction, RabbitMQMessagingTransaction>();

    ServiceProvider = services.BuildServiceProvider();
  }

  // --- Dispose container + DI safely ---
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