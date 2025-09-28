using Confluent.Kafka;
using Franz.Common.Messaging.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Franz.Common.Messaging.Kafka;

public class KafkaListener : IListener
{
  private readonly IConsumer<string, string> _consumer;
  private readonly ILogger<KafkaListener> _logger;
  private readonly string[] _topics;

  public KafkaListener(
      IConsumer<string, string> consumer,
      IEnumerable<string> topics,
      ILogger<KafkaListener> logger)
  {
    _consumer = consumer;
    _topics = topics.ToArray();
    _logger = logger;
  }

  public event EventHandler<MessageEventArgs>? Received;

  public Task Listen(CancellationToken stoppingToken = default)
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
          result = _consumer.Consume(); // respect cancellation
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

        Received?.Invoke(this, new MessageEventArgs(transport));
      }
    }
    finally
    {
      _consumer.Close();
    }

    return Task.CompletedTask;
  }


  public void StopListen() => _consumer.Unsubscribe();
}
