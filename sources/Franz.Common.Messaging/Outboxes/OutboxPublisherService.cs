using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Outbox;

public class OutboxPublisherService : BackgroundService
{
  private readonly IMessageStore _messageStore;
  private readonly IMessagingSender _sender;
  private readonly ILogger<OutboxPublisherService> _logger;
  private readonly OutboxOptions _options;

  public OutboxPublisherService(
      IMessageStore messageStore,
      IMessagingSender sender,
      ILogger<OutboxPublisherService> logger,
      OutboxOptions options)
  {
    _messageStore = messageStore;
    _sender = sender;
    _logger = logger;
    _options = options;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    if (!_options.Enabled)
    {
      _logger.LogInformation("OutboxPublisherService is disabled.");
      return;
    }

    _logger.LogInformation("OutboxPublisherService started with polling interval {PollingInterval}", _options.PollingInterval);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var pendingMessages = await _messageStore.GetPendingAsync(stoppingToken);

        foreach (var msg in pendingMessages)
        {
          try
          {
          
            await _sender.SendAsync(msg.ToMessage(), stoppingToken);
            await _messageStore.MarkAsSentAsync(msg.Id, stoppingToken);

            _logger.LogInformation("Message {MessageId} sent successfully.", msg.Id);
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Failed to send message {MessageId}", msg.Id);
            // TODO: increment retry count, move to dead-letter if needed
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during outbox polling.");
      }

      await Task.Delay(_options.PollingInterval, stoppingToken);
    }
  }
}
