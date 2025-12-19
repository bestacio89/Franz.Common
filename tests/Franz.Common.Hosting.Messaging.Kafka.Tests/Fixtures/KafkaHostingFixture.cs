using Confluent.Kafka;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Hosting.Kafka;
using Franz.Common.Messaging.Kafka.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Kafka;

public sealed class KafkaHostingFixture
  : HostedMessagingFixture<KafkaContainer>
{
  public string BootstrapServers => Container!.GetBootstrapAddress();

  protected override KafkaContainer CreateContainer()
    => new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.6.1")
        .WithCleanUp(true)
        .Build();

  protected override IHost BuildHost(KafkaContainer container)
  {
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Messaging:BootStrapServers"] = container.GetBootstrapAddress(),
        ["Messaging:GroupID"] = "franz-test-group"
      })
      .Build();

    return new HostBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();

        services.AddFranzMediator(new[]
        {
          typeof(KafkaHostingFixture).Assembly
        });
        services.AddSingleton<IConsumer<string, string>>(sp =>
        {
          var config = sp.GetRequiredService<KafkaConsumerConfiguration>();

          return new ConsumerBuilder<string, string>(config.ToConfluent())
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();
        });
        services.AddKafkaMessaging(configuration);

        services.AddKafkaHostedListener(options =>
        {
          options.BootStrapServers = container.GetBootstrapAddress();
          options.GroupID = "franz-test-group";
        });
      })
      .Build();
  }
}
