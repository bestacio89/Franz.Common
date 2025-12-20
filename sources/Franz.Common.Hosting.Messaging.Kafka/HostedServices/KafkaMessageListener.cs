using Confluent.Kafka;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Franz.Common.Messaging.Hosting.Kafka.HostedServices;

public sealed class KafkaMessageListener : IListener
{
  private readonly IConsumer<string, string> _consumer;
  private readonly ILogger<KafkaMessageListener> _logger;
  private readonly string[] _topics;
  private readonly bool _awaitHandlers;

  public KafkaMessageListener(
      IConsumer<string, string> consumer,
      IEnumerable<string> topics,
      ILogger<KafkaMessageListener> logger,
      bool awaitHandlers = false) // 🔥 prod = fire-and-forget, tests = true
  {
    _consumer = consumer;
    _topics = topics.ToArray();
    _logger = logger;
    _awaitHandlers = awaitHandlers;
  }

  public event EventHandler<MessageEventArgs>? Received;

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    _consumer.Subscribe(_topics);
    _logger.LogInformation(
      "🎧 Kafka listener subscribed to {Topics} (AwaitHandlers={Await})",
      string.Join(",", _topics),
      _awaitHandlers);

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        ConsumeResult<string, string>? result;

        try
        {
          result = _consumer.Consume(stoppingToken);
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "❌ Kafka consume error");
          continue;
        }

        if (result?.Message?.Value is null)
        {
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

        if (Received != null)
        {
          var handlers = Received
            .GetInvocationList()
            .Cast<EventHandler<MessageEventArgs>>()
            .ToArray();

          if (_awaitHandlers)
          {
            // 🧪 TEST MODE — deterministic execution
            var tasks = handlers.Select(handler =>
              Task.Run(() =>
              {
                try
                {
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
              }, stoppingToken));

            await Task.WhenAll(tasks);
          }
          else
          {
            // 🚀 PROD MODE — fire-and-forget
            foreach (var handler in handlers)
            {
              _ = Task.Run(() =>
              {
                try
                {
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
              }, stoppingToken);
            }
          }
        }

        // ✅ Commit AFTER dispatch (but not after completion in prod)
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
