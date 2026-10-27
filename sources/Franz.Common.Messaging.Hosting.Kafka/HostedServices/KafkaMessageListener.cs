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
    _consumer.Subscribe(_topics);
    _logger.LogInformation("Kafka listener subscribed to {Topics}", string.Join(", ", _topics));

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        ConsumeResult<string, string> result;
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
          _logger.LogError(ex, "Kafka consume error");
          throw;
        }

        // A Kafka tombstone (null value) is a valid record. There is no payload
        // to dispatch, so it can be acknowledged after recording the skip.
        if (result.Message?.Value is null)
        {
          _logger.LogDebug("Skipping Kafka tombstone on {Topic} partition {Partition} offset {Offset}",
            result.Topic, result.Partition, result.Offset);
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
          _logger.LogError(ex, "Kafka envelope deserialization failed on {Topic} partition {Partition} offset {Offset}",
            result.Topic, result.Partition, result.Offset);
          // Do not advance the offset: the invalid record remains observable and
          // must be handled through the configured operational/DLT policy.
          throw;
        }

        if (transport is null)
        {
          var error = new InvalidOperationException(
            $"Kafka envelope deserialized to null at {result.Topic}/{result.Partition}/{result.Offset}.");
          _logger.LogError(error, "Kafka envelope was null");
          throw error;
        }

        var handler = OnMessageReceivedAsync
          ?? throw new InvalidOperationException("Kafka message handler has not been configured.");

        try
        {
          // Sequential processing keeps commit order aligned with processing order.
          await handler(new MessageEventArgs(transport)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex,
            "Kafka handler failed for message {MessageId} on {Topic} partition {Partition} offset {Offset}; offset will not be committed",
            transport.Id, result.Topic, result.Partition, result.Offset);
          throw;
        }

        // Commit only after successful handler completion. With auto commit/store
        // disabled, this provides at-least-once delivery (duplicates are possible).
        _consumer.Commit(result);
      }
    }
    finally
    {
      _consumer.Close();
      _logger.LogInformation("Kafka listener stopped");
    }
  }

  public Task StopListenAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      _logger.LogInformation("Kafka consumer unsubscribing");
      _consumer.Unsubscribe();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during Kafka unsubscribe");
      return Task.FromException(ex);
    }

    return Task.CompletedTask;
  }
}