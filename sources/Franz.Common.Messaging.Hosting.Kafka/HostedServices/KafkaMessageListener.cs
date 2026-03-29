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
    _logger.LogInformation(
        "🎧 Kafka listener subscribed to {Topics}",
        string.Join(", ", _topics));

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
          // ✅ Delegate to IMessageSerializer abstraction — JsonSerializer
          // is no longer a direct dependency of this class. The concrete
          // implementation is resolved from DI via AddDefaultMessageSerializer().
          transport = _serializer.Deserialize<Message>(result.Message.Value);
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

        if (OnMessageReceivedAsync is not null)
        {
          try
          {
            // Awaiting ensures the handler (including scope disposal) completes
            // before moving to the next message — at-least-once guarantee.
            await OnMessageReceivedAsync(new MessageEventArgs(transport));
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

        // ✅ Commit ONLY after the handler has finished — never before.
        _consumer.Commit(result);
      }
    }
    finally
    {
      _consumer.Close();
      _logger.LogInformation("🛑 Kafka listener stopped");
    }
  }

  public Task StopListenAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      _logger.LogInformation("📤 Kafka consumer unsubscribing...");
      _consumer.Unsubscribe();
      _logger.LogInformation("✅ Kafka consumer unsubscribed.");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "❌ Error during Kafka unsubscribe.");
      return Task.FromException(ex);
    }

    return Task.CompletedTask;
  }
}