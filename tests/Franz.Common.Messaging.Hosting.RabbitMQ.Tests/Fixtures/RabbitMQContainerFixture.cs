#nullable enable
using DotNet.Testcontainers.Builders;
using Testcontainers.RabbitMq;
using Xunit;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;

public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
  private readonly string _user = "guest";
  private readonly string _password = "guest";

  public string QueueName { get; } = $"franz-test-{Guid.CreateVersion7():N}";

  public RabbitMqContainer Container { get; } =
      new RabbitMqBuilder("rabbitmq:4.0-management")
          .WithUsername("guest")
          .WithPassword("guest")
          .WithCleanUp(true)
          .Build();

  /// <summary>
  /// Returns the full AMQP URI for the container.
  /// Format: amqp://guest:guest@localhost:port/
  /// </summary>
  public string ConnectionString => Container.GetConnectionString();

  public async Task InitializeAsync() => await Container.StartAsync();

  public async Task DisposeAsync() => await Container.DisposeAsync();

  /// <summary>
  /// Generates a dictionary compatible with IConfiguration for Messaging:RabbitMQ.
  /// Prioritizes URI-based BootstrapServers over legacy HostName/Port.
  /// </summary>
  public Dictionary<string, string?> GetConfiguration()
  {
    return new Dictionary<string, string?>
    {
      //Map directly to BootStrapServers to test the URI parsing logic
      ["Messaging:RabbitMQ:BootStrapServers"] = ConnectionString,
      ["Messaging:RabbitMQ:UserName"] = _user,
      ["Messaging:RabbitMQ:Password"] = _password,
      ["Messaging:RabbitMQ:ExchangeName"] = "franz-test-exchange",
      ["Messaging:RabbitMQ:DefaultRoutingKey"] = "test-key",
      ["Messaging:RabbitMQ:QueueName"] = QueueName,
      ["Messaging:RabbitMQ:VirtualHost"] = "/",
      ["Messaging:RabbitMQ:AutomaticRecoveryEnabled"] = "true",
      ["Messaging:RabbitMQ:TopologyRecoveryEnabled"] = "true"
    };
  }
}