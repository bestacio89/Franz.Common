using Confluent.Kafka;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Franz.Common.Messaging.Hosting.Kafka.HostedServices;

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
    _consumer = consumer;
    _topics = topics.ToArray();
    _logger = logger;
  }

  public event EventHandler<MessageEventArgs>? Received;

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    _consumer.Subscribe(_topics);
    _logger.LogInformation(
      "🎧 Kafka listener subscribed to {Topics}",
      string.Join(",", _topics));

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        ConsumeResult<string, string>? result = null;

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
          _logger.LogError(ex, "❌ Kafka consume error");
          continue;
        }

        if (result?.Message?.Value is null)
        {
          if (result is not null)
            _consumer.Commit(result);
          continue;
        }

        Message? transport;
        try
        {
          transport = JsonSerializer.Deserialize<Message>(result.Message.Value);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(
            ex,
            "⚠️ Failed to deserialize Kafka message on topic {Topic}",
            result.Topic);

          _consumer.Commit(result);
          continue;
        }

        if (transport is null)
        {
          _consumer.Commit(result);
          continue;
        }

        if (Received is not null)
        {
          var handlers = Received
            .GetInvocationList()
            .Cast<EventHandler<MessageEventArgs>>()
            .ToArray();

          foreach (var handler in handlers)
          {
            try
            {
              // IMPORTANT:
              // Do NOT offload with Task.Run + stoppingToken.
              // This must execute deterministically in tests.
              handler(this, new MessageEventArgs(transport));
            }
            catch (Exception ex)
            {
              _logger.LogError(
                ex,
                "🔥 Kafka handler failed for message {MessageId} on topic {Topic}",
                transport.Id,
                result.Topic);
            }
          }
        }

        // ✅ Commit after dispatch (at-least-once)
        _consumer.Commit(result);
      }
    }
    finally
    {
      _consumer.Close();
      _logger.LogInformation("🛑 Kafka listener stopped");
    }
  }

  public void StopListen() => _consumer.Unsubscribe();
}
