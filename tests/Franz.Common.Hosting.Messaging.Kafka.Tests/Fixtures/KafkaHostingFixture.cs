using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting.Kafka;
using Franz.Common.Messaging.Kafka.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Franz.Common.Mediator.Extensions;
using System.Reflection;
public sealed class KafkaHostingFixture : IAsyncDisposable
{
  public IHost Host { get; }

  public KafkaHostingFixture(string bootstrapServers)
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:BootStrapServers"] = bootstrapServers,
          ["Messaging:GroupID"] = "franz-test-group"
        })
        .Build();

    Host = new HostBuilder()
        .ConfigureServices(services =>
        {
          services.AddLogging();
          services.AddFranzMediator( new [] {Assembly.GetCallingAssembly()});

          // Core messaging (binds MessagingOptions from IConfiguration)
          services.AddKafkaMessaging(configuration);

          // Hosted listener (explicit options, same values)
          services.AddKafkaHostedListener(options =>
          {
            options.BootStrapServers = bootstrapServers;
            options.GroupID = "franz-test-group";
          });
        })
        .Build();
  }

  public Task StartAsync() => Host.StartAsync();

  public async ValueTask DisposeAsync()
  {
    await Host.StopAsync();
    Host.Dispose();
  }
}
