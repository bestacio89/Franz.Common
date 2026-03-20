using Confluent.Kafka;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Validation.Events;
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
          // 1. --- TEST PROBES ---
          services.AddSingleton<ITestProbe, TestProbe>();
          services.AddSingleton<ITestPipelineProbe, TestPipelineProbe>();
          services.AddLogging();

          // 2. --- MEDIATOR SETUP ---
          services.AddFranzMediator(new[] { typeof(KafkaHostingFixture).Assembly });

          //3. --- SCOPE ISOLATION ---
          services.AddScoped<IEventHandler<ScopeTestEvent>, ScopeTrackingHandler>();
          services.AddTransient(typeof(IEventPipeline<>), typeof(TestEventPipeline<>));

          // 4. --- PRODUCTION KAFKA TRANSPORT ---
          services.AddKafkaMessaging(configuration);

          // 5. --- TOPIC INITIALIZER ---
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
                            "UnhandledTestEvent",
                            "franz-test-in-dlt" // 🛠️ Explicitly create DLT
                    });
          });

          // 6. --- NATIVE CONSUMER (For DLT Verification Test) ---
          services.AddSingleton<IConsumer<string, string>>(_ =>
          {
            var consumerConfig = new ConsumerConfig
            {
              BootstrapServers = container.GetBootstrapAddress(),
              GroupId = $"{groupId}-native-verifier", // Unique group for the test consumer
              AutoOffsetReset = AutoOffsetReset.Earliest,
              EnableAutoCommit = true
            };

            return new ConsumerBuilder<string, string>(consumerConfig)
                    .SetKeyDeserializer(Deserializers.Utf8)
                    .SetValueDeserializer(Deserializers.Utf8)
                    .Build();
          });

          // 7. --- THE ENGINE (Fixes Scope Isolation Failure) ---
          // We use the real MessagingHostedService because it contains the 
          // .CreateScope() logic we need to test.
          services.AddHostedService<MessagingHostedService>();
        })
        .Build();
  }

  // --- CI TIMEOUT FIXES ---

  public async Task StartHostAsync()
  {
    // CI environments are slow; give the Host 30s to connect to Docker Kafka
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await Host!.StartAsync(cts.Token);
  }

  public async Task StopHostAsync()
  {
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await Host!.StopAsync(cts.Token);
  }

  // --- PRODUCER HELPERS ---

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