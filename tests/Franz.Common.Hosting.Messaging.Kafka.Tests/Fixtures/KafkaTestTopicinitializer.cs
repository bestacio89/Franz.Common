
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Fixtures;


internal sealed class KafkaTestTopicInitializer : IHostedService
{
  private readonly string _bootstrapServers;
  private readonly string[] _topics;

  public KafkaTestTopicInitializer(string bootstrapServers, string[] topics)
  {
    _bootstrapServers = bootstrapServers;
    _topics = topics;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    using var admin = new AdminClientBuilder(
      new AdminClientConfig { BootstrapServers = _bootstrapServers })
      .Build();

    var specs = _topics.Select(t => new TopicSpecification
    {
      Name = t,
      NumPartitions = 1,          // 🔑 THIS IS THE KEY
      ReplicationFactor = 1
    });

    try
    {
      await admin.CreateTopicsAsync(specs);
    }
    catch (CreateTopicsException e)
      when (e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
    {
      // Ignore
    }
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

