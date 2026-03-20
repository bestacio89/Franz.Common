#nullable enable
using System.Threading.Tasks;
using Testcontainers.Kafka;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Fixtures;

public sealed class KafkaContainerFixture : IAsyncLifetime
{
  private readonly KafkaContainer _container;

  public string BootstrapServers => _container.GetBootstrapAddress();

  public KafkaContainerFixture()
  {
    // Use the constructor that takes the Docker image explicitly
    _container = new KafkaBuilder("confluentinc/cp-kafka:7.6.1")
        .WithCleanUp(true)
        .Build();
  }

  public async Task InitializeAsync() => await _container.StartAsync();

  public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("Kafka")]
public class KafkaCollection : ICollectionFixture<KafkaContainerFixture> { }