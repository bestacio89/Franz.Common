using Confluent.Kafka;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Franz.Common.Messaging.Hosting.Kafka.HostedServices;

/// <summary>
/// Kafka transport listener responsible for consuming messages
/// and dispatching them to registered handlers in an isolated manner.
/// </summary>
public sealed class KafkaMessageListener : IListener
{
  private readonly IConsumer<string, string> _consumer;
  private readonly ILogger<KafkaMessageListener> _logger;
  private readonly string[] _topics;

  public KafkaMessageListener(
      IConsumer<string, string> consumer,
      IEnumerable<string> topics,
      ILogger<KafkaMessageListener> logger)
  {
    _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    _topics = topics?.ToArray() ?? throw new ArgumentNullException(nameof(topics));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public event EventHandler<MessageEventArgs>? Received;

  /// <summary>
  /// Starts consuming Kafka messages and dispatching them
  /// to registered handlers. Handler failures are isolated.
  /// </summary>
  public async Task Listen(CancellationToken stoppingToken = default)
  {
    _consumer.Subscribe(_topics);
    _logger.LogInformation(
      "🎧 Kafka listener subscribed to topics: {Topics}",
      string.Join(", ", _topics));

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        ConsumeResult<string, string>? result;

        try
        {
          result = _consumer.Consume(stoppingToken);
        }
        catch (OperationCanceledException)
        {
          // Expected during shutdown
          break;
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "❌ Kafka consume error");
          continue;
        }

        if (result?.Message?.Value is null)
          continue;

        Message? transport;

        try
        {
          transport = JsonSerializer.Deserialize<Message>(result.Message.Value);
        }
        catch (JsonException ex)
        {
          _logger.LogError(
            ex,
            "❌ Failed to deserialize Kafka message from topic {Topic}",
            result.Topic);
          continue;
        }

        if (transport is null)
        {
          _logger.LogWarning(
            "⚠️ Skipped message: deserialized message was null (Topic: {Topic})",
            result.Topic);
          continue;
        }

        if (Received is null)
          continue;

        var args = new MessageEventArgs(transport);

        foreach (var handler in Received
                   .GetInvocationList()
                   .Cast<Func<object, MessageEventArgs, Task>>())
        {
          try
          {
            await handler(this, args);
          }
          catch (Exception ex)
          {
            _logger.LogError(
              ex,
              "❌ Kafka handler failed while processing message {MessageId}",
              transport.Id);
          }
        }
      }
    }
    finally
    {
      _consumer.Close();
      _logger.LogInformation("🛑 Kafka listener stopped");
    }
  }

  public void StopListen()
  {
    _consumer.Unsubscribe();
  }
}
