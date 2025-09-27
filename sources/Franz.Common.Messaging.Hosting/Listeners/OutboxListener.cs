using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Adapters;
using Franz.Common.Messaging.Outbox;
using global::Franz.Common.Mediator.Dispatchers;
using global::Franz.Common.Mediator.Messages;
using global::Franz.Common.Messaging.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.HostingMessaging.Listeners;

public class OutboxMessageListener : BackgroundService
{
  private readonly IMessageStore _messageStore;
  private readonly IDispatcher _dispatcher;
  private readonly ILogger<OutboxMessageListener> _logger;

  public OutboxMessageListener(
      IMessageStore messageStore,
      IDispatcher dispatcher,
      ILogger<OutboxMessageListener> logger)
  {
    _messageStore = messageStore;
    _dispatcher = dispatcher;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("Outbox listener started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var pending = await _messageStore.GetPendingAsync(stoppingToken);

        foreach (StoredMessage stored in pending)
        {
          var message = stored.ToMessage();

          try
          {
            if (message.ToEvent() is IEvent @event)
            {
              await _dispatcher.PublishAsync(@event, stoppingToken);
            }
            else if (message.ToCommand() is ICommand command)
            {
              await _dispatcher.SendAsync(command, stoppingToken);
            }
            else
            {
              _logger.LogWarning("Unknown message type {Type}, skipping", message.MessageType);
            }

            await _messageStore.MarkAsSentAsync(stored.Id, stoppingToken);
          }
          catch (Exception ex)
          {
            _logger.LogError(ex,
                "Error dispatching message {Id} with type {Type}",
                stored.Id,
                message.MessageType);
            // TODO: retry counters / dead-letter handling
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error polling outbox.");
      }

      // throttle a bit to avoid hammering DB
      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }

    _logger.LogInformation("Outbox listener stopped.");
  }
}
