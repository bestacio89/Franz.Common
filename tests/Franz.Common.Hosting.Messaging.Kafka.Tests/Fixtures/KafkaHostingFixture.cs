using Confluent.Kafka;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Validation.Events;
using Franz.Common.Messaging.Hosting.Kafka;
using Franz.Common.Messaging.Hosting.Kafka.HostedServices;
using Franz.Common.Messaging.Kafka.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using Testcontainers.Kafka;

public sealed class KafkaHostingFixture : HostedMessagingFixture<KafkaContainer>
{
  public string BootstrapServers => Container!.GetBootstrapAddress();

  protected override KafkaContainer CreateContainer()
      => new KafkaBuilder()
          .WithImage("confluentinc/cp-kafka:7.6.1")
          .WithCleanUp(true)
          .Build();

  protected override IHost BuildHost(KafkaContainer container)
  {
    // 🔑 UNIQUE GROUP PER FIXTURE INSTANCE to avoid cross-test interference
    var groupId = $"franz-test-group-{Guid.NewGuid():N}";

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:BootStrapServers"] = container.GetBootstrapAddress(),
          ["Messaging:GroupID"] = groupId
        })
        .Build();

    return new HostBuilder()
        .ConfigureServices(services =>
        {
          // --- TEST PROBES ---
          services.AddSingleton<ITestProbe, TestProbe>();
          services.AddSingleton<ITestPipelineProbe, TestPipelineProbe>();
          services.AddLogging();

          // --- MEDIATOR SETUP ---
          services.AddFranzMediator(new[]
              {
                    typeof(KafkaHostingFixture).Assembly
            });

          // Register the Test Pipeline (Open Generic)
          services.AddTransient(typeof(IEventPipeline<>), typeof(TestEventPipeline<>));

          // Register the Scope Tracking Handler
          services.AddTransient<IEventHandler<ScopeTestEvent>, ScopeTrackingHandler>();

          // --- KAFKA TRANSPORT SETUP ---
          services.AddKafkaMessaging(configuration);

          // Topic Initializer to ensure topics exist before the consumer starts
          services.AddSingleton<IHostedService>(sp =>
          {
            return new KafkaTestTopicInitializer(
                    container.GetBootstrapAddress(),
                    topics: new[]
                    {
                            "FaultToleranceTestEvent",
                            "FanoutTestEvent",
                            "TestEvent",
                            "ScopeTestEvent",
                            "UnhandledTestEvent"
                    });
          });

          // 🔥 Native Kafka consumer for low-level verification
          services.AddSingleton<IConsumer<string, string>>(_ =>
          {
            var consumerConfig = new ConsumerConfig
            {
              BootstrapServers = container.GetBootstrapAddress(),
              GroupId = groupId,
              AutoOffsetReset = AutoOffsetReset.Earliest,
              EnableAutoCommit = true
            };

            return new ConsumerBuilder<string, string>(consumerConfig)
                    .SetKeyDeserializer(Deserializers.Utf8)
                    .SetValueDeserializer(Deserializers.Utf8)
                    .Build();
          });

          // 🔥 The actual Franz Listener components
          services.AddSingleton<KafkaMessageListener>();
          services.AddHostedService<KafkaHostedService>();
        })
        .Build();
  }

  // --- LIFECYCLE CONTROL ---

  public async Task StopHostAsync() => await Host!.StopAsync();

  public async Task StartHostAsync() => await Host!.StartAsync();

  // --- RAW PRODUCTION FOR EDGE CASES ---

  public async Task ProduceRawMessageAsync(string topic, string rawContent)
  {
    var config = new ProducerConfig { BootstrapServers = BootstrapServers };
    using var producer = new ProducerBuilder<string, string>(config).Build();
    await producer.ProduceAsync(topic, new Message<string, string>
    {
      Key = Guid.NewGuid().ToString(),
      Value = rawContent
    });
  }

  // Supports the Correlation ID / Metadata tests
  public async Task ProduceWithHeaderAsync(string topic, string content, string headerKey, string headerValue)
  {
    var config = new ProducerConfig { BootstrapServers = BootstrapServers };
    using var producer = new ProducerBuilder<string, string>(config).Build();

    var headers = new Headers { { headerKey, Encoding.UTF8.GetBytes(headerValue) } };

    await producer.ProduceAsync(topic, new Message<string, string>
    {
      Key = Guid.NewGuid().ToString(),
      Value = content,
      Headers = headers
    });
  }
}