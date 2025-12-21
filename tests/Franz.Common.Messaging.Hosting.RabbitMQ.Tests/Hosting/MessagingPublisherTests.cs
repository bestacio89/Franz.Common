using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fakes;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Hosting;

public class MessagingPublisherTests
  : IClassFixture<RabbitMqContainerFixture>
{
  private readonly RabbitMqContainerFixture _fixture;

  public MessagingPublisherTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task MessagingPublisher_invokes_IMessageHandler()
  {
    TestMessageHandler.LastMessage = null;

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
        services.AddRabbitMQMessagingPublisher(config);
        services.AddMessagingSerialization();
        // override default delegating handler
        services.AddSingleton<IMessageHandler, TestMessageHandler>();
      })
      .Build();

    await host.StartAsync();

    var publisher = host.Services.GetRequiredService<IMessagingPublisher>();
    await publisher.Publish(new TestIntegrationEvent(Guid.NewGuid()));

    Assert.NotNull(TestMessageHandler.LastMessage);
    Assert.Equal("franz-test-correlation",
      TestMessageHandler.LastMessage!.CorrelationId);

    await host.StopAsync();
  }
}

