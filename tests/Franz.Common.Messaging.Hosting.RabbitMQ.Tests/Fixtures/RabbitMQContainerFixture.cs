#nullable enable
using DotNet.Testcontainers.Builders;
using Testcontainers.RabbitMq;
using Xunit;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;

public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
  private const string _user = "guest";
  private const string _password = "guest";

  public const string Image = "rabbitmq:4.0-management";

  public string QueueName { get; } = $"franz-test-{Guid.CreateVersion7():N}";

  public RabbitMqContainer Container { get; } =
      new RabbitMqBuilder(Image)
          .WithUsername(_user)
          .WithPassword(_password)
          .WithCleanUp(true)
          .WithWaitStrategy(Wait.ForUnixContainer()
              .UntilInternalTcpPortIsAvailable(5672))
                    .Build();

  public string ConnectionString => Container.GetConnectionString();

  public async Task InitializeAsync() => await Container.StartAsync();

  public async Task DisposeAsync() => await Container.DisposeAsync();

  public Dictionary<string, string?> GetConfiguration()
  {
    return new Dictionary<string, string?>
    {
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
