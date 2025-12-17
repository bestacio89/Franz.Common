using DotNet.Testcontainers.Configurations;

using Testcontainers.Kafka;
using Xunit;
using Xunit.Sdk;

public sealed class KafkaContainerFixture : IAsyncLifetime
{
  private readonly KafkaContainer _container;

  public string BootstrapServers => _container.GetBootstrapAddress();

  public KafkaContainerFixture()
  {
    _container = new KafkaBuilder()
      .WithImage("confluentinc/cp-kafka:7.6.1")
      .WithCleanUp(true)
      .Build();
  }

  public async Task InitializeAsync()
    => await _container.StartAsync();

  public async Task DisposeAsync()
    => await _container.DisposeAsync();
}