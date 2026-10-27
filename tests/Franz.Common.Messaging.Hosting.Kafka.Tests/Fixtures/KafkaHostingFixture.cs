#nullable enable
using System.Text;
using Confluent.Kafka;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Validation.Events;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.Kafka.HostedServices;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Events;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Executing;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Handlers;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Franz.Common.Messaging.Hosting.Listeners;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Serialization;
using Franz.Common.MongoDB.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.Kafka;
using Testcontainers.MongoDb;
using Xunit;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Fixtures;

public sealed class KafkaHostingFixture : IAsyncLifetime
{
  private readonly KafkaContainer _kafka;
  private readonly MongoDbContainer _mongo;

  public IHost? Host { get; private set; }
  public string BootstrapServers => _kafka.GetBootstrapAddress();
  public string MongoConnectionString => _mongo.GetConnectionString();
  public string MongoDatabaseName { get; } = $"franz-tests-{Guid.CreateVersion7():N}";

  public KafkaHostingFixture()
  {
    _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.4.0").WithCleanUp(true).Build();
    _mongo = new MongoDbBuilder("mongo:7.0").WithCleanUp(true).Build();
  }

  public async Task InitializeAsync()
  {
    await Task.WhenAll(_kafka.StartAsync(), _mongo.StartAsync());
    Host = BuildHost();
    await Host.StartAsync();
  }

  public async Task DisposeAsync()
  {
    if (Host != null)
    {
      await Host.StopAsync();
      Host.Dispose();
    }

    await Task.WhenAll(_kafka.DisposeAsync().AsTask(), _mongo.DisposeAsync().AsTask());
  }

  public IServiceProvider Services =>
    Host?.Services ?? throw new InvalidOperationException("Host not started");

  private IHost BuildHost()
  {
    var groupId = $"franz-test-group-{Guid.CreateVersion7():N}";
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Messaging:Kafka:BootstrapServers"] = BootstrapServers,
        ["Messaging:Kafka:GroupId"] = groupId,
        ["Messaging:Kafka:Failure:DeadLetterTopic"] = "hero-test-dlt",
        ["Messaging:Kafka:Failure:MaxRetryAttempts"] = "1",
        ["Messaging:Kafka:Failure:RetryBackoffMs"] = "0",
        ["Mongo:ConnectionString"] = MongoConnectionString,
        ["Mongo:Database"] = MongoDatabaseName
      })
      .Build();

    return new HostBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();
        services.AddSingleton<ITestProbe, TestProbe>();
        services.AddSingleton<ITestPipelineProbe, TestPipelineProbe>();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddFranzMediator(new[] { typeof(KafkaHostingFixture).Assembly });
        services.AddScoped<IEventHandler<ScopeTestEvent>, ScopeTrackingHandler>();
        services.AddTransient(typeof(IEventPipeline<>), typeof(TestEventPipeline<>));

        services.AddKafkaMessaging(configuration);

        var testTopics = new[]
        {
          "FaultToleranceTestEvent",
          "FanoutTestEvent",
          "TestEvent",
          "ScopeTestEvent",
          "hero-test-dlt"
        };

        services.AddSingleton<IHostedService>(_ =>
          new KafkaTestTopicInitializer(BootstrapServers, testTopics));

        services.AddSingleton<KafkaMessageListener>(sp =>
          new KafkaMessageListener(
              sp.GetRequiredService<IConsumer<string, string>>(),
              testTopics.Where(t => t != "hero-test-dlt"),
              sp.GetRequiredService<IMessageSerializer>(),
              sp.GetRequiredService<ILogger<KafkaMessageListener>>(),
              sp.GetRequiredService<IProducer<string, byte[]>>(),
              sp.GetRequiredService<IOptions<KafkaMessagingOptions>>()));

        services.AddSingleton<IListener>(sp => sp.GetRequiredService<KafkaMessageListener>());
        services.AddScoped<IMessagingStrategyExecuter, TestMessagingStrategyExecuter>();
        services.AddMongoMessageStore(MongoConnectionString, MongoDatabaseName);
        services.AddSingleton<OutboxMessageListener>();
        services.AddHostedService<KafkaMessagingHostedService>();
        services.AddHostedService<OutboxHostedService>();
      })
      .Build();
  }

  public async Task ProduceRawMessageAsync(string topic, string rawContent)
  {
    var config = new ProducerConfig { BootstrapServers = BootstrapServers };
    using var producer = new ProducerBuilder<string, string>(config).Build();
    await producer.ProduceAsync(topic, new Message<string, string>
    {
      Key = Guid.CreateVersion7().ToString(),
      Value = rawContent
    });
    producer.Flush(TimeSpan.FromSeconds(5));
  }

  public async Task<ConsumeResult<string, byte[]>?> ConsumeDeadLetterAsync(
      string topic,
      string expectedPayload,
      TimeSpan timeout)
  {
    var config = new ConsumerConfig
    {
      BootstrapServers = BootstrapServers,
      GroupId = $"dlt-test-{Guid.CreateVersion7():N}",
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false
    };

    using var consumer = new ConsumerBuilder<string, byte[]>(config)
      .SetValueDeserializer(Deserializers.ByteArray)
      .Build();
    consumer.Subscribe(topic);

    var deadline = DateTimeOffset.UtcNow + timeout;
    while (DateTimeOffset.UtcNow < deadline)
    {
      var result = consumer.Consume(TimeSpan.FromMilliseconds(250));
      if (result?.Message?.Value is not null &&
          Encoding.UTF8.GetString(result.Message.Value) == expectedPayload)
      {
        return result;
      }

      await Task.Yield();
    }

    return null;
  }
}
