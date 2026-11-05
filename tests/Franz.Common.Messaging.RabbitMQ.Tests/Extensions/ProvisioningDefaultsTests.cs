#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Extensions;

public sealed class RabbitMqProvisioningDefaultsTests
{
  [Theory]
  [InlineData("Development", ProvisioningMode.Create)]
  [InlineData("Production", ProvisioningMode.Verify)]
  [InlineData("Staging", ProvisioningMode.Verify)]
  public void UnspecifiedProvisioningMode_UsesEnvironmentSafeDefault(string environment, ProvisioningMode expected)
  {
    var configuration = CreateConfiguration();

    using var host = new HostBuilder()
        .UseEnvironment(environment)
        .ConfigureServices((_, services) => services.AddRabbitMqMessagingOptions(configuration))
        .Build();

    host.Services.GetRequiredService<IOptions<RabbitMQMessagingOptions>>()
        .Value.ProvisioningMode.Should().Be(expected);
  }

  [Fact]
  public void ExplicitProvisioningMode_OverridesEnvironmentDefault()
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:RabbitMQ:BootStrapServers"] = "amqp://service:secret@localhost:5672/",
          ["Messaging:RabbitMQ:ProvisioningMode"] = "Off"
        })
        .Build();

    using var host = new HostBuilder()
        .UseEnvironment(Environments.Production)
        .ConfigureServices((_, services) => services.AddRabbitMqMessagingOptions(configuration))
        .Build();

    host.Services.GetRequiredService<IOptions<RabbitMQMessagingOptions>>()
        .Value.ProvisioningMode.Should().Be(ProvisioningMode.Off);
  }

  private static IConfiguration CreateConfiguration()
      => new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string?>
          {
            ["Messaging:RabbitMQ:BootStrapServers"] = "amqp://service:secret@localhost:5672/",
            ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = "5",
            ["Messaging:RabbitMQ:SslEnabled"] = "false"
          })
          .Build();
}