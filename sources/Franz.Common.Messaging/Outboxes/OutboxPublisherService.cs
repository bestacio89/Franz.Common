using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Outbox;

public class OutboxPublisherService(
    IMessageStore messageStore,
    IMessagingSender sender,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisherService> logger)
    : BackgroundService
{
  private readonly IMessageStore _messageStore = messageStore;
  private readonly IMessagingSender _sender = sender;
  private readonly OutboxOptions _options = options.Value;
  private readonly ILogger<OutboxPublisherService> _logger = logger;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("🚀 OutboxPublisherService started with polling interval {Interval} ms and max retries {MaxRetries}",
      _options.PollingInterval.TotalMilliseconds, _options.MaxRetries);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var pending = await _messageStore.GetPendingAsync(stoppingToken);

        if (pending.Count == 0)
        {
          _logger.LogDebug("📭 No pending messages found in outbox");
        }

        foreach (var stored in pending)
        {
          try
          {
            var message = stored.ToMessage();
            _logger.LogInformation("📤 Sending message {MessageId} of type {MessageType} (Retry {RetryCount})",
              stored.Id, stored.MessageType, stored.RetryCount);

            await _sender.SendAsync(message, stoppingToken);
            await _messageStore.MarkAsSentAsync(stored.Id, stoppingToken);

            _logger.LogInformation("✅ Successfully sent message {MessageId}", stored.Id);
          }
          catch (Exception ex)
          {
            stored.RetryCount++;
            stored.LastError = ex.Message;
            stored.LastTriedOn = DateTime.UtcNow;

            if (stored.RetryCount >= _options.MaxRetries)
            {
              await _messageStore.MoveToDeadLetterAsync(stored, stoppingToken);
              _logger.LogError(ex,
                "💀 Message {MessageId} moved to DeadLetter after {Retries} retries. Last error: {Error}",
                stored.Id, stored.RetryCount, stored.LastError);
            }
            else
            {
              await _messageStore.UpdateRetryAsync(stored, stoppingToken);
              _logger.LogWarning(ex,
                "⚠️ Message {MessageId} failed attempt {RetryCount}. Error: {Error}. Will retry.",
                stored.Id, stored.RetryCount, stored.LastError);
            }
          }
        }
      }
      catch (Exception loopEx)
      {
        _logger.LogError(loopEx, "🔥 OutboxPublisherService main loop failed unexpectedly");
      }

      await Task.Delay(_options.PollingInterval, stoppingToken);
    }

    _logger.LogInformation("🛑 OutboxPublisherService stopped");
  }
}
