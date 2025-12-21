using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Hosting;

public class RabbitMqMessageModelTests
  : IClassFixture<RabbitMqContainerFixture>
{
  private readonly RabbitMqContainerFixture _fixture;

  public RabbitMqMessageModelTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task ProduceAsync_publishes_without_exception()
  {
    var services = new ServiceCollection();

    services.Configure<MessagingOptions>(opts =>
    {
      opts.HostName = _fixture.Host;
      opts.Port = _fixture.Port;
    });

    services.AddSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>();
    services.AddSingleton<IConnectionProvider, ConnectionProvider>();
    services.AddScoped<IRabbitMqMessageModel, RabbitMqMessageModel>();

    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();

    var model = scope.ServiceProvider.GetRequiredService<IRabbitMqMessageModel>();
    var connection = scope.ServiceProvider.GetRequiredService<IConnectionProvider>().Current;

    using var channel = await connection.CreateChannelAsync();
    await channel.ExchangeDeclareAsync(
      exchange: "franz.tests.exchange",
      type: ExchangeType.Fanout,
      durable: false,
      autoDelete: true);

    await model.ProduceAsync(
      exchange: "franz.tests.exchange",
      routingKey: "",
      message: new { Value = "hello-franz" });
  }
}
