using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
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
  private readonly RabbitMqContainerFixture _rabbit;

  public RabbitMqHostedServiceTests(RabbitMqContainerFixture fixture)
  {
    _rabbit = fixture;
  }
  private IConfiguration BuildRabbitConfiguration()
  {
    return new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Messaging:HostName"] = _rabbit.Host,
        ["Messaging:Port"] = _rabbit.Port.ToString()
      })
      .Build();
  }
  [Fact]
  public async Task RabbitMQHostedService_starts_and_stops()
  {
    var configuration = BuildRabbitConfiguration();

    // 🔑 REQUIRED INFRA
 
    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();
        services.AddMessagingSerialization();
        services.AddRabbitMQMessaging(configuration);
        // 🔑 RabbitMQ messaging stack (THIS WAS MISSING)
        services.AddRabbitMQMessagingConfiguration(new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string?>
          {
            ["Messaging:HostName"] = _rabbit.Host,
            ["Messaging:Port"] = _rabbit.Port.ToString()
          })
          .Build());
        services.AddFranzMediator(new[]{
          typeof(TestIntegrationEvent).Assembly});

        services.AddRabbitMQHostedListener(opts =>
        {
          opts.HostName = _rabbit.Host;
          opts.Port = _rabbit.Port;
        });
      })
      .Build();

    await host.StartAsync();
    await host.StopAsync();
  }

}
