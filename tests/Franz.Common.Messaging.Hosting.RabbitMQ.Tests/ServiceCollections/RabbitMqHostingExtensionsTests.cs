using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.ServiceCollections;

using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting.Listeners;
using Franz.Common.Messaging.Hosting.RabbitMQ;
using Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.Outbox;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

public class RabbitMQHostingExtensionsTests
  : IClassFixture<RabbitMqContainerFixture>
{
  private readonly RabbitMqContainerFixture _fixture;

  public RabbitMQHostingExtensionsTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public void AddRabbitMQHostedListener_registers_listener_and_hosted_service()
  {
    var services = new ServiceCollection();

    services.AddRabbitMQHostedListener(opts =>
    {
      opts.HostName = _fixture.Host;
      opts.Port = _fixture.Port;
    });

    var provider = services.BuildServiceProvider();

    var listener = provider.GetService<Listener>();
    Assert.NotNull(listener);

    var hostedServices = provider.GetServices<IHostedService>();
    Assert.Contains(hostedServices,
      s => s.GetType() == typeof(RabbitMQHostedService));
  }

  [Fact]
  public void AddRabbitMQHostedListener_binds_MessagingOptions()
  {
    var services = new ServiceCollection();

    services.AddRabbitMQHostedListener(opts =>
    {
      opts.HostName = "rabbit-test";
      opts.Port = 5678;
    });

    var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<
      Microsoft.Extensions.Options.IOptions<MessagingOptions>>().Value;

    Assert.Equal("rabbit-test", options.HostName);
    Assert.Equal(5678, options.Port);
  }

  [Fact]
  public async Task RabbitMQHostedService_starts_and_stops()
  {
    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();

        services.AddRabbitMQHostedListener(opts =>
        {
          opts.HostName = _fixture.Host;
          opts.Port = _fixture.Port;
        });
      })
      .Build();

    await host.StartAsync();
    await host.StopAsync();
  }
  [Fact]
  public void AddOutboxHostedListener_registers_outbox_listener_and_service()
  {
    var services = new ServiceCollection();

    services.AddOutboxHostedListener(opts =>
    {
      opts.PollingInterval = TimeSpan.FromMilliseconds(100);
    });

    var provider = services.BuildServiceProvider();

    var listener = provider.GetService<OutboxMessageListener>();
    Assert.NotNull(listener);

    var hostedServices = provider.GetServices<IHostedService>();
    Assert.Contains(hostedServices,
      s => s.GetType() == typeof(OutboxHostedService));
  }

  [Fact]
  public async Task OutboxHostedService_starts_and_stops()
  {
    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();
        services.AddOutboxHostedListener(opts =>
        {
          opts.PollingInterval = TimeSpan.FromMilliseconds(100);
        });
      })
      .Build();

    await host.StartAsync();
    await host.StopAsync();
  }
}
