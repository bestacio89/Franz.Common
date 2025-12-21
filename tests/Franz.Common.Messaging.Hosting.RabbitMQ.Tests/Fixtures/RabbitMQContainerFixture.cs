using Testcontainers.RabbitMq;
using Xunit;
namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;

public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
  public RabbitMqContainer Container { get; } =
    new RabbitMqBuilder()
      .WithImage("rabbitmq:3.12-management")
      .WithUsername("guest")
      .WithPassword("guest")
      .Build();

  public string Host => Container.Hostname;
  public int Port => Container.GetMappedPublicPort(5672);

  public async Task InitializeAsync()
    => await Container.StartAsync();

  public async Task DisposeAsync()
    => await Container.DisposeAsync();
}
