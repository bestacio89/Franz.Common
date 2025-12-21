using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Hosting;

public class ModelProviderTests
  : IClassFixture<RabbitMqContainerFixture>
{
  private readonly RabbitMqContainerFixture _fixture;

  public ModelProviderTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public void ModelProvider_creates_single_channel()
  {
    var services = new ServiceCollection();

    services.Configure<MessagingOptions>(opts =>
    {
      opts.HostName = _fixture.Host;
      opts.Port = _fixture.Port;
    });

    services.AddSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>();
    services.AddSingleton<IConnectionProvider, ConnectionProvider>();
    services.AddScoped<IModelProvider, ModelProvider>();

    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();

    var model = scope.ServiceProvider.GetRequiredService<IModelProvider>();

    var ch1 = model.Current;
    var ch2 = model.Current;

    Assert.Same(ch1, ch2);
    Assert.True(ch1.IsOpen);
  }
}

