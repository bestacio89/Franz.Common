#nullable enable
using DotNet.Testcontainers.Builders;
using Testcontainers.Kafka;
using Xunit;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Fixtures;

public sealed class KafkaContainerFixture : IAsyncLifetime
{
  // Franz.Common Standard: Use a consistent ID for the test suite
  // or generate one per test run to avoid partition lock-in.
  public string DefaultGroupId { get; } = $"franz-test-group-{Guid.NewGuid():N}";

  public KafkaContainer Container { get; }

  public string BootstrapServers => Container.GetBootstrapAddress();

  public KafkaContainerFixture()
  {
    Container = new KafkaBuilder("confluentinc/cp-kafka:7.4.0")
         // WSL 2.7 Optimization: Ensure the port is ready for the bridge
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(9092))
        .WithCleanUp(true)
        .Build();
  }

  public async Task InitializeAsync() => await Container.StartAsync();

  public async Task DisposeAsync() => await Container.DisposeAsync();

  /// <summary>
  /// Helper to provide pre-configured consumer settings for tests.
  /// </summary>
  public Dictionary<string, string> GetConsumerConfig(string? overrideGroupId = null)
  {
    return new Dictionary<string, string>
        {
            { "bootstrap.servers", BootstrapServers },
            { "group.id", overrideGroupId ?? DefaultGroupId },
            { "auto.offset.reset", "earliest" },
            { "enable.auto.commit", "false" }
        };
  }
}