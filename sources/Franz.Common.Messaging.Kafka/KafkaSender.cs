#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Franz.Common.Messaging.Kafka.Senders;

public sealed class KafkaSender(
    IOptions<KafkaMessagingOptions> messagingOptions,
    IMessageSerializer serializer,
    IAssemblyAccessor assemblyAccessor,
    ILogger<KafkaSender> logger) : IMessagingSender, IAsyncDisposable, IDisposable
{
  private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(
      new ProducerConfig
      {
        BootstrapServers = messagingOptions.Value.BootStrapServers,
        EnableIdempotence = true,
        Acks = Acks.All,
        MessageSendMaxRetries = int.MaxValue
      }).Build();

  private int _disposed = 0;

  // Senior Architect Note: Using ValueTask to reduce heap allocations on the messaging hot-path.
  public async ValueTask SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (Volatile.Read(ref _disposed) == 1)
      throw new ObjectDisposedException(nameof(KafkaSender));

    try
    {
      var kafkaMessage = new Message<string, string>
      {
        Key = message.CorrelationId.ToString(),
        Value = serializer.Serialize(message.Body),
        Headers = new Confluent.Kafka.Headers()
      };

      // Mapping Architectural Headers
      kafkaMessage.Headers.Add("X-Message-ID", Encoding.UTF8.GetBytes(message.Id.ToString()));

      foreach (var header in message.Headers)
      {
        foreach (var value in header.Value)
        {
          if (value is not null)
          {
            kafkaMessage.Headers.Add(header.Key, Encoding.UTF8.GetBytes(value));
          }
        }
      }

      var topic = TopicNamer.GetTopicName(assemblyAccessor.GetEntryAssembly());

      // Kafka Client's ProduceAsync returns a Task, which is safely wrapped by ValueTask
      await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

      logger.LogDebug("[Franz.Messaging] Produced message {MessageId} to {Topic}",
          message.Id, topic);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "[Franz.Messaging] Failed to produce message {MessageId}",
          message.Id);
      throw;
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

    await Task.Run(() =>
    {
      _producer.Flush(TimeSpan.FromSeconds(10));
      _producer.Dispose();
    });

    GC.SuppressFinalize(this);
  }

  public void Dispose()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

    _producer.Flush(TimeSpan.FromSeconds(10));
    _producer.Dispose();

    GC.SuppressFinalize(this);
  }
}