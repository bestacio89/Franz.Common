using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fakes;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Microsoft.Extensions.Configuration;
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

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Messaging:HostName"] = _fixture.Host,
        ["Messaging:Port"] = _fixture.Port.ToString()
      })
      .Build();
    services.AddFranzMediator(new[]{
          typeof(TestIntegrationEvent).Assembly});
    services.AddRabbitMQMessaging(config);
    services. AddSingleton<IRabbitMqMessageModel, RabbitMqMessageModel>();
    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();

    var model = scope.ServiceProvider
      .GetRequiredService<IRabbitMqMessageModel>();

    await model.ProduceAsync(
      exchange: "franz.tests.exchange",
      routingKey: "",
      message: new { Value = "hello-franz" });
  }
}
