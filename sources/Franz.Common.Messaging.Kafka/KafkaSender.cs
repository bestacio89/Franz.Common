#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Transactions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Threading;

namespace Franz.Common.Messaging.Kafka.Senders;

public sealed class KafkaSender : IMessagingSender, IAsyncDisposable, IDisposable
{
  private readonly IProducer<string, byte[]> _producer;
  private readonly IMessageSerializer _serializer;
  private readonly KafkaTopicNames _topicNames;
  private readonly ILogger<KafkaSender> _logger;
  private readonly KafkaMessageKeyStrategy _keyStrategy;
  private readonly KafkaTransactionContext _transactionContext;
  private int _disposed;

  public KafkaSender(
      IProducer<string, byte[]> producer,
      IMessageSerializer serializer,
      KafkaTopicNames topicNames,
      IOptions<KafkaMessagingOptions> options,
      ILogger<KafkaSender> logger,
      KafkaTransactionContext transactionContext)
  {
    _producer = producer ?? throw new ArgumentNullException(nameof(producer));
    _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    _topicNames = topicNames ?? throw new ArgumentNullException(nameof(topicNames));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _keyStrategy = ResolveKeyStrategy(options);
    _transactionContext = transactionContext ?? throw new ArgumentNullException(nameof(transactionContext));
  }

  public async ValueTask SendAsync(Message message, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    if (Volatile.Read(ref _disposed) == 1)
      throw new ObjectDisposedException(nameof(KafkaSender));

    try
    {
      var key = ResolveMessageKey(message, _keyStrategy);
      var kafkaMessage = new Message<string, byte[]> { Key = key, Value = Serialize(message), Headers = BuildHeaders(message) };
      var producer = _transactionContext.Producer ?? _producer;
      var result = await producer.ProduceAsync(_topicNames.TopicName, kafkaMessage, ct).ConfigureAwait(false);
      _logger.LogDebug("[Kafka] Delivered {MessageId} to {Topic} [Partition: {Partition}, Offset: {Offset}]",
          message.Id, result.Topic, result.Partition.Value, result.Offset.Value);
    }
    catch (ProduceException<string, byte[]> ex)
    {
      _logger.LogError(ex, "[Kafka] Produce failure {MessageId} | Reason: {Reason}", message.Id, ex.Error.Reason);
      throw;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "[Kafka] Unexpected error producing {MessageId}", message.Id);
      throw;
    }
  }

  private static KafkaMessageKeyStrategy ResolveKeyStrategy(IOptions<KafkaMessagingOptions> options)
  {
    var settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    return settings.OrderingStrategy switch
    {
      null => settings.Producer.MessageKeyStrategy,
      MessageOrderingStrategy.None => KafkaMessageKeyStrategy.None,
      MessageOrderingStrategy.MessageId => KafkaMessageKeyStrategy.MessageId,
      MessageOrderingStrategy.CorrelationId => KafkaMessageKeyStrategy.CorrelationId,
      MessageOrderingStrategy.AggregateId => KafkaMessageKeyStrategy.AggregateId,
      _ => throw new ArgumentOutOfRangeException(nameof(options), settings.OrderingStrategy, "Unsupported message ordering strategy.")
    };
  }

  private static string? ResolveMessageKey(Message message, KafkaMessageKeyStrategy strategy)
      => strategy switch
      {
        KafkaMessageKeyStrategy.None => null,
        KafkaMessageKeyStrategy.MessageId => message.Id.ToString(),
        KafkaMessageKeyStrategy.CorrelationId => message.CorrelationId?.ToString()
            ?? throw new InvalidOperationException($"Kafka message {message.Id} has no CorrelationId, but the CorrelationId key strategy is configured."),
        KafkaMessageKeyStrategy.AggregateId => message.AggregateId?.ToString()
            ?? throw new InvalidOperationException($"Kafka message {message.Id} has no AggregateId, but the AggregateId key strategy is configured."),
        _ => throw new InvalidOperationException($"Unsupported Kafka message key strategy: {strategy}.")
      };

  private byte[] Serialize(Message message)
  {
    var value = _serializer.Serialize(message.Body);
    return string.IsNullOrEmpty(value) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(value);
  }

  private static Confluent.Kafka.Headers BuildHeaders(Message message)
  {
    var headers = new Confluent.Kafka.Headers { { "X-Message-ID", Encoding.UTF8.GetBytes(message.Id.ToString()) } };
    foreach (var (key, values) in message.Headers)
    {
      if (values is null || values.Length == 0) continue;
      foreach (var value in values)
        if (!string.IsNullOrWhiteSpace(value))
          headers.Add(key, Encoding.UTF8.GetBytes(value));
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