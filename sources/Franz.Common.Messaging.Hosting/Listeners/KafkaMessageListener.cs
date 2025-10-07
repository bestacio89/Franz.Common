using Confluent.Kafka;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Franz.Common.Messaging.Hosting.Listeners;

public class KafkaMessageListener : IListener
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
    _logger.LogInformation("🎧 Kafka listener subscribed to {Topics}", string.Join(",", _topics));

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
          _logger.LogError(ex, "❌ Error consuming Kafka message");
          continue;
        }

        if (result?.Message?.Value is null)
          continue;

        var transport = JsonSerializer.Deserialize<Message>(result.Message.Value);
        if (transport is null)
        {
          _logger.LogWarning("⚠️ Skipped message: cannot deserialize transport for {Topic}", result.Topic);
          continue;
        }

        if (Received != null)
        {
          var handlers = Received.GetInvocationList().Cast<Func<object, MessageEventArgs, Task>>();
          await Task.WhenAll(handlers.Select(h => h(this, new MessageEventArgs(transport))));
        }
      }
    }
    finally
    {
      _consumer.Close();
    }
  }

  public void StopListen() => _consumer.Unsubscribe();
}
