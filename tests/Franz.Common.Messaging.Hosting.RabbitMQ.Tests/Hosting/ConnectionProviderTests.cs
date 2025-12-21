using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Hosting;

public class ConnectionProviderTests
  : IClassFixture<RabbitMqContainerFixture>
{
  private readonly RabbitMqContainerFixture _fixture;

  public ConnectionProviderTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public void ConnectionProvider_creates_single_open_connection()
  {
    var services = new ServiceCollection();

    services.Configure<MessagingOptions>(opts =>
    {
      opts.HostName = _fixture.Host;
      opts.Port = _fixture.Port;
    });

    services.AddSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>();
    services.AddSingleton<IConnectionProvider, ConnectionProvider>();

    using var provider = services.BuildServiceProvider();

    var c1 = provider.GetRequiredService<IConnectionProvider>().Current;
    var c2 = provider.GetRequiredService<IConnectionProvider>().Current;

    Assert.Same(c1, c2);
    Assert.True(c1.IsOpen);
  }
}
