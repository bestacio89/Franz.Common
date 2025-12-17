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
        .WithImage("bitnami/kafka:3.7")
        .WithEnvironment("ALLOW_PLAINTEXT_LISTENER", "yes")
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
