using Confluent.Kafka;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Adapters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Franz.Common.HostingMessaging.Kafka;

public class KafkaMessageListener : BackgroundService
{
  private readonly IConsumer<string, string> _consumer;
  private readonly IDispatcher _mediator;
  private readonly ILogger<KafkaMessageListener> _logger;
  private readonly string[] _topics;

  public KafkaMessageListener(
    IConsumer<string, string> consumer,
    IDispatcher mediator,
    ILogger<KafkaMessageListener> logger,
    IEnumerable<string> topics)
  {
    _consumer = consumer;
    _mediator = mediator;
    _logger = logger;
    _topics = topics.ToArray();
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _consumer.Subscribe(_topics);
    _logger.LogInformation("Kafka listener subscribed to {Topics}", string.Join(",", _topics));

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        ConsumeResult<string, string>? result = null;
        try
        {
          result = _consumer.Consume(stoppingToken);
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Error consuming Kafka message");
          continue;
        }

        if (result?.Message?.Value is null)
          continue;

        try
        {
          // Deserialize transport-level message
          var transport = JsonSerializer.Deserialize<Message>(result.Message.Value);
          if (transport is null)
          {
            _logger.LogWarning("Skipped message: cannot deserialize transport for {Topic}", result.Topic);
            continue;
          }

          // Try event first
          var ev = transport.ToEvent();
          if (ev is not null)
          {
            _logger.LogInformation("Dispatching Event {EventType} (CorrelationId={CorrelationId})",
              ev.GetType().Name, transport.CorrelationId);

            await _mediator.PublishAsync(ev, stoppingToken);
            continue;
          }

          // Try command
          var cmd = transport.ToCommand();
          if (cmd is not null)
          {
            _logger.LogInformation("Dispatching Command {CommandType} (CorrelationId={CorrelationId})",
              cmd.GetType().Name, transport.CorrelationId);

            await _mediator.SendAsync(cmd, stoppingToken);
            continue;
          }

          _logger.LogWarning("Unhandled message type {MessageType}", transport.MessageType);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error handling message from topic {Topic}", result.Topic);
        }
      }
    }
    finally
    {
      _consumer.Close();
    }
  }
}
