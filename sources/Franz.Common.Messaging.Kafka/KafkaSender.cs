#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Threading;

namespace Franz.Common.Messaging.Kafka.Senders;

public sealed class KafkaSender(
    IProducer<string, byte[]> producer,
    IMessageSerializer serializer,
    IAssemblyAccessor assemblyAccessor,
    IOptions<KafkaMessagingOptions> options,
    ILogger<KafkaSender> logger)
    : IMessagingSender, IAsyncDisposable, IDisposable
{
  private readonly KafkaMessageKeyStrategy _keyStrategy =
      options?.Value?.Producer?.MessageKeyStrategy
      ?? throw new ArgumentNullException(nameof(options));

  private int _disposed;

  public async ValueTask SendAsync(Message message, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (Volatile.Read(ref _disposed) == 1)
      throw new ObjectDisposedException(nameof(KafkaSender));

    try
    {
      var key = ResolveMessageKey(message, _keyStrategy);
      var kafkaMessage = new Message<string, byte[]>
      {
        Key = key,
        Value = Serialize(message),
        Headers = BuildHeaders(message)
      };

      var topic = TopicNamer.GetTopicName(assemblyAccessor.GetEntryAssembly());
      var result = await producer.ProduceAsync(topic, kafkaMessage, ct).ConfigureAwait(false);

      logger.LogDebug(
          "[Kafka] Delivered {MessageId} to {Topic} [Partition: {Partition}, Offset: {Offset}]",
          message.Id, result.Topic, result.Partition.Value, result.Offset.Value);
    }
    catch (ProduceException<string, byte[]> ex)
    {
      logger.LogError(ex, "[Kafka] Produce failure {MessageId} | Reason: {Reason}",
          message.Id, ex.Error.Reason);
      throw;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "[Kafka] Unexpected error producing {MessageId}", message.Id);
      throw;
    }
  }

  private static string ResolveMessageKey(Message message, KafkaMessageKeyStrategy strategy)
      => strategy switch
      {
        KafkaMessageKeyStrategy.MessageId => message.Id.ToString(),
        KafkaMessageKeyStrategy.CorrelationId => message.CorrelationId?.ToString()
            ?? throw new InvalidOperationException(
                $"Kafka message {message.Id} has no CorrelationId, but the CorrelationId key strategy is configured."),
        KafkaMessageKeyStrategy.AggregateId => message.AggregateId?.ToString()
            ?? throw new InvalidOperationException(
                $"Kafka message {message.Id} has no AggregateId, but the AggregateId key strategy is configured."),
        _ => throw new InvalidOperationException($"Unsupported Kafka message key strategy: {strategy}.")
      };

  private byte[] Serialize(Message message)
  {
    var value = serializer.Serialize(message.Body);
    return string.IsNullOrEmpty(value) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(value);
  }

  private static Confluent.Kafka.Headers BuildHeaders(Message message)
  {
    var headers = new Confluent.Kafka.Headers
    {
      { "X-Message-ID", Encoding.UTF8.GetBytes(message.Id.ToString()) }
    };

    foreach (var (key, values) in message.Headers)
    {
      if (values is null || values.Length == 0)
        continue;

      foreach (var value in values)
      {
        if (!string.IsNullOrWhiteSpace(value))
          headers.Add(key, Encoding.UTF8.GetBytes(value));
      }
    }

    return headers;
  }

  public ValueTask DisposeAsync()
  {
    Dispose();
    return ValueTask.CompletedTask;
  }

  public void Dispose()
  {
    Interlocked.Exchange(ref _disposed, 1);
    GC.SuppressFinalize(this);
  }
}