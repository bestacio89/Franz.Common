using Testcontainers.Kafka;
using Xunit;

public sealed class KafkaContainerFixture : IAsyncLifetime
{
  private readonly KafkaContainer _container;

  public string BootstrapServers => _container.GetBootstrapAddress();

  public KafkaContainerFixture()
  {
    _container = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.6.1")
        .Build();
  }

  public async Task InitializeAsync()
  {
    await _container.StartAsync();
  }

  public async Task DisposeAsync()
  {
    await _container.DisposeAsync();
  }
}
