#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Kafka.HostedServices;

public sealed class KafkaMessageListener : IListener
{
  private readonly IConsumer<string, string> _consumer;
  private readonly ILogger<KafkaMessageListener> _logger;
  private readonly IMessageSerializer _serializer;
  private readonly string[] _topics;

  public KafkaMessageListener(
      IConsumer<string, string> consumer,
      IEnumerable<string> topics,
      IMessageSerializer serializer,
      ILogger<KafkaMessageListener> logger)
  {
    _consumer = consumer;
    _topics = topics.ToArray();
    _serializer = serializer;
    _logger = logger;
  }

  public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    if (OnMessageReceivedAsync is null)
      throw new InvalidOperationException("A message handler must be configured before the Kafka listener starts.");

    if (_topics.Length == 0 || _topics.Any(string.IsNullOrWhiteSpace))
      throw new InvalidOperationException("At least one non-empty Kafka topic must be configured.");

    _consumer.Subscribe(_topics);
    _logger.LogInformation("Kafka listener subscribed to {Topics}", string.Join(", ", _topics));

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        ConsumeResult<string, string>? result;
        try
        {
          result = _consumer.Consume(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
          break;
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Kafka consume failed; the listener will continue polling.");
          continue;
        }

        if (result is null)
          continue;

        // A null Kafka value is a valid tombstone for compacted topics. It is
        // deliberately ignored and committed; it is not a serialized Franz message.
        if (result.Message?.Value is null)
        {
          _logger.LogDebug("Ignoring Kafka tombstone on topic {Topic} partition {Partition} offset {Offset}.",
              result.Topic, result.Partition.Value, result.Offset.Value);
          _consumer.Commit(result);
          continue;
        }

        Message? transport;
        try
        {
          transport = _serializer.Deserialize<Message>(result.Message.Value);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Kafka message at {Topic}[{Partition}] offset {Offset} could not be deserialized. The offset is not committed.",
              result.Topic, result.Partition.Value, result.Offset.Value);
          throw new InvalidDataException(
              $"Kafka message at {result.Topic}[{result.Partition.Value}] offset {result.Offset.Value} could not be deserialized.",
              ex);
        }

        if (transport is null)
        {
          _logger.LogError("Kafka message at {Topic}[{Partition}] offset {Offset} deserialized to null. The offset is not committed.",
              result.Topic, result.Partition.Value, result.Offset.Value);
          throw new InvalidDataException(
              $"Kafka message at {result.Topic}[{result.Partition.Value}] offset {result.Offset.Value} deserialized to null.");
        }

        try
        {
          await OnMessageReceivedAsync(new MessageEventArgs(transport));
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Kafka handler failed for message {MessageId} at {Topic}[{Partition}] offset {Offset}. The offset is not committed.",
              transport.Id, result.Topic, result.Partition.Value, result.Offset.Value);
          throw;
        }

        // Commit only after the full handler pipeline has completed successfully.
        _consumer.Commit(result);
      }
    }
    finally
    {
      _consumer.Close();
      _logger.LogInformation("Kafka listener stopped.");
    }
  }

  public Task StopListenAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      _logger.LogInformation("Kafka consumer unsubscribing.");
      _consumer.Unsubscribe();
      return Task.CompletedTask;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Kafka consumer unsubscribe failed.");
      return Task.FromException(ex);
    }
  }
}
