#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading;

namespace Franz.Common.Messaging.Kafka.Senders;

public sealed class KafkaSender(
    IProducer<string, byte[]> producer,
    IMessageSerializer serializer,
    IAssemblyAccessor assemblyAccessor,
    ILogger<KafkaSender> logger)
    : IMessagingSender, IAsyncDisposable, IDisposable
{
  private int _disposed;

  public async ValueTask SendAsync(Message message, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (Volatile.Read(ref _disposed) == 1)
      throw new ObjectDisposedException(nameof(KafkaSender));

    try
    {
      // =========================
      // NORMALIZE TRANSPORT VALUES
      // =========================
      var correlationKey =
        message.CorrelationId?.ToString()
        ?? message.Id.ToString();

      var payload = Serialize(message);

      var kafkaMessage = new Message<string, byte[]>
      {
        Key = correlationKey,
        Value = payload,
        Headers = BuildHeaders(message)
      };

      var topic = TopicNamer.GetTopicName(assemblyAccessor.GetEntryAssembly());

      var result = await producer
          .ProduceAsync(topic, kafkaMessage, ct)
          .ConfigureAwait(false);

      logger.LogDebug(
          "[Kafka] Delivered {MessageId} to {Topic} [Partition: {Partition}, Offset: {Offset}]",
          message.Id,
          result.Topic,
          result.Partition.Value,
          result.Offset.Value);
    }
    catch (ProduceException<string, byte[]> ex)
    {
      logger.LogError(ex,
          "[Kafka] Produce failure {MessageId} | Reason: {Reason}",
          message.Id,
          ex.Error.Reason);

      throw;
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
          "[Kafka] Unexpected error producing {MessageId}",
          message.Id);

      throw;
    }
  }

  // =========================
  // HOT PATH HELPERS
  // =========================

  private byte[] Serialize(Message message)
  {
    var str = serializer.Serialize(message.Body);

    if (string.IsNullOrEmpty(str))
      return Array.Empty<byte>();

    return Encoding.UTF8.GetBytes(str);
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
        {
          headers.Add(key, Encoding.UTF8.GetBytes(value));
        }
      }
    }

    return headers;
  }

  // =========================
  // LIFECYCLE
  // =========================

  public async ValueTask DisposeAsync()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1)
      return;

    try
    {
      producer.Flush(TimeSpan.FromSeconds(5));
    }
    catch
    {
      // ignore shutdown errors
    }

    producer.Dispose();
    GC.SuppressFinalize(this);

    await ValueTask.CompletedTask;
  }

  public void Dispose()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1)
      return;

    try
    {
      producer.Flush(TimeSpan.FromSeconds(5));
    }
    catch
    {
      // ignore shutdown errors
    }

    producer.Dispose();
    GC.SuppressFinalize(this);
  }
}