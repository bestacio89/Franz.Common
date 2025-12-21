using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fakes;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Hosting;

public class RabbitMqHostedServiceTests
  : IClassFixture<RabbitMqContainerFixture>
{
  private readonly RabbitMqContainerFixture _fixture;

  public RabbitMqHostedServiceTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task HostedService_starts_and_stops_cleanly()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Messaging:HostName"] = _fixture.Host,
        ["Messaging:Port"] = _fixture.Port.ToString()
      })
      .Build();

    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();
        services.AddRabbitMQMessagingConsumer(config);
        services.AddRabbitMQHostedListener(_ => { });
      })
      .Build();

    await host.StartAsync();
    await host.StopAsync();
  }
}
