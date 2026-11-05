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
  public const string ManagementApiUsername = "franz-test";
  public const string ManagementApiPassword = "franz-test-management-password";

  public RabbitMqContainer Container { get; } = new RabbitMqBuilder(Image)
      .WithUsername(ManagementApiUsername)
      .WithPassword(ManagementApiPassword)
      .WithCleanUp(true)
      .WithPortBinding(15672, true)
      .WithWaitStrategy(Wait.ForUnixContainer()
          .UntilInternalTcpPortIsAvailable(5672)
          .UntilInternalTcpPortIsAvailable(15672))
            .Build();

  public string ConnectionString { get; private set; } = default!;
  public string ManagementApiUrl => $"http://{Container.Hostname}:{Container.GetMappedPublicPort(15672)}";
  public ServiceProvider ServiceProvider { get; private set; } = default!;

  public Task InitializeAsync() => InitializeAsyncInternal();

  private async Task InitializeAsyncInternal()
  {
    try
    {
      await Container.StartAsync();
      ConnectionString = Container.GetConnectionString();

      var services = new ServiceCollection();
      services.AddSingleton(Options.Create(new RabbitMQMessagingOptions
    {
      BootStrapServers = ConnectionString,
      ManagementApiUrl = ManagementApiUrl,
      UserName = ManagementApiUsername,
      Password = ManagementApiPassword,
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
    catch
    {
      await Container.DisposeAsync().ConfigureAwait(false);
      throw;
    }
  }

  public async Task DisposeAsync()
  {
    var serviceProvider = ServiceProvider;
    ServiceProvider = null!;

    try
    {
      if (serviceProvider is not null)
        await serviceProvider.DisposeAsync().ConfigureAwait(false);
    }
    finally
    {
      await Container.DisposeAsync().ConfigureAwait(false);
    }
  }
}
