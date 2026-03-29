#nullable enable
using Confluent.Kafka;
using DotNet.Testcontainers.Builders;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.Kafka;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Fixtures;

public sealed class KafkaContainerFixture : IAsyncLifetime
{
  private readonly KafkaContainer _container = new KafkaBuilder("confluentinc/cp-kafka:7.4.0")
      .WithCleanUp(true)
      .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(".*ready to handle requests.*"))
      .Build();

  public string BootstrapServers => _container.GetBootstrapAddress();

  public async Task InitializeAsync() => await _container.StartAsync();

  public async Task DisposeAsync()
  {
    await _container.StopAsync();
    await _container.DisposeAsync();
  }

  /// <summary>
  /// Creates a ServiceProvider with all required Franz.Common infrastructure
  /// to prevent registration "flops" due to missing cross-package dependencies.
  /// </summary>
  public IServiceProvider BuildServiceProvider(Action<IServiceCollection>? configure = null)
  {
    var services = new ServiceCollection();

    // 1. Setup Configuration
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:Kafka:BootStrapServers"] = BootstrapServers,
          ["Messaging:Kafka:GroupID"] = "integration-test-group",
          ["Messaging:Kafka:TopicName"] = "integration-test-topic"
        })
        .Build();

    services.AddSingleton<IConfiguration>(configuration);

    // 2. Core Infrastructure (Required by Kafka Extensions)
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
    services.AddFranzMediator(new[] { typeof(KafkaContainerFixture).Assembly });
    services.AddNoDuplicateScoped<IAssemblyAccessor, AssemblyAccessorWrapper>();

    // 3. Messaging Foundations (Serialization, Factories)
    // These are often called internally by AddKafkaMessaging
    services.AddDefaultMessageSerializer();

    // 4. Kafka Transport Layer
    services.AddKafkaMessaging(configuration);

    // 5. Custom Overrides/Additions
    configure?.Invoke(services);

    return services.BuildServiceProvider();
  }

  /// <summary>
  /// Helper to grab options directly from the container
  /// </summary>
  public KafkaMessagingOptions GetOptions(IServiceProvider sp)
      => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaMessagingOptions>>().Value;
}