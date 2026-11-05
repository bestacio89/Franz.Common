#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Configuration;

public sealed class KafkaProvisioningDefaultsTests
{
  [Theory]
  [InlineData("Development", ProvisioningMode.Create)]
  [InlineData("Production", ProvisioningMode.Verify)]
  [InlineData("Staging", ProvisioningMode.Verify)]
  public void UnspecifiedProvisioningMode_UsesEnvironmentSafeDefault(string environment, ProvisioningMode expected)
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:Kafka:BootstrapServers"] = "localhost:9092",
          ["Messaging:Kafka:GroupId"] = "test-group",
          ["Messaging:Kafka:Security:SecurityProtocol"] = "Ssl"
        })
        .Build();

    using var host = new HostBuilder()
        .UseEnvironment(environment)
        .ConfigureServices((_, services) => services.AddKafkaMessagingOptions(configuration))
        .Build();

    host.Services.GetRequiredService<IOptions<KafkaMessagingOptions>>()
        .Value.ProvisioningMode.Should().Be(expected);
  }

  [Fact]
  public void ExplicitProvisioningMode_OverridesEnvironmentDefault()
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:Kafka:BootstrapServers"] = "localhost:9092",
          ["Messaging:Kafka:GroupId"] = "test-group",
          ["Messaging:Kafka:Security:SecurityProtocol"] = "Ssl",
          ["Messaging:Kafka:ProvisioningMode"] = "Off"
        })
        .Build();

    using var host = new HostBuilder()
        .UseEnvironment("Production")
        .ConfigureServices((_, services) => services.AddKafkaMessagingOptions(configuration))
        .Build();

    host.Services.GetRequiredService<IOptions<KafkaMessagingOptions>>()
        .Value.ProvisioningMode.Should().Be(ProvisioningMode.Off);
  }
}