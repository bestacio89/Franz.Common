#nullable enable
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
    _logger.LogInformation("🚀 OutboxPublisherService started. Interval: {Interval}ms, MaxRetries: {MaxRetries}",
        _options.PollingInterval.TotalMilliseconds, _options.MaxRetries);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        // Due to Guid v7, GetPendingAsync should return messages sorted by Id
        var pending = await _messageStore.GetPendingAsync(stoppingToken);

        if (pending.Count == 0)
        {
          _logger.LogDebug("📭 No pending messages found in outbox");
        }

        foreach (var stored in pending)
        {
          try
          {
            // 1. Re-hydrate the native Message (Guid v7 IDs preserved)
            var message = stored.ToMessage();

            _logger.LogInformation("📤 Sending message {MessageId} of type {MessageType} | Correlation: {CorrelationId}",
                stored.Id, stored.MessageType, stored.CorrelationId);

            // 2. Dispatch to the hardened sender (Kafka, Service Bus, etc.)
            await _sender.SendAsync(message, stoppingToken);

            // 3. Mark success using the native Guid ID
            await _messageStore.MarkAsSentAsync(stored.Id, stoppingToken);

            _logger.LogInformation("✅ Successfully sent message {MessageId}", stored.Id);
          }
          catch (Exception ex)
          {
            // 4. Update retry metadata
            stored.RetryCount++;
            stored.LastError = ex.Message;
            stored.LastTriedOn = DateTime.UtcNow;

            if (stored.RetryCount >= _options.MaxRetries)
            {
              await _messageStore.MoveToDeadLetterAsync(stored, stoppingToken);
              _logger.LogError(ex, "💀 Message {MessageId} moved to DLQ after {Retries} retries.",
                  stored.Id, stored.RetryCount);
            }
            else
            {
              await _messageStore.UpdateRetryAsync(stored, stoppingToken);
              _logger.LogWarning(ex, "⚠️ Message {MessageId} failed (Attempt {RetryCount}/{MaxCount}). Will retry.",
                  stored.Id, stored.RetryCount, _options.MaxRetries);
            }
          }
        }
      }
      catch (Exception loopEx)
      {
        _logger.LogError(loopEx, "🔥 OutboxPublisherService main loop failed unexpectedly");
      }

      // Respect the polling interval from options
      await Task.Delay(_options.PollingInterval, stoppingToken);
    }

    _logger.LogInformation("🛑 OutboxPublisherService stopped");
  }
}