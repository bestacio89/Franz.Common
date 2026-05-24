#nullable enable
using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Outbox;

public sealed class OutboxPublisherService : BackgroundService
{
  private readonly IMessageStore _messageStore;
  private readonly IMessagingSender _sender;
  private readonly OutboxOptions _options;
  private readonly ILogger<OutboxPublisherService> _logger;

  public OutboxPublisherService(
      IMessageStore messageStore,
      IMessagingSender sender,
      IOptions<OutboxOptions> options,
      ILogger<OutboxPublisherService> logger)
  {
    _messageStore = messageStore;
    _sender = sender;
    _options = options.Value;
    _logger = logger;
  }

  // =====================================================
  // HOSTED LOOP (ORCHESTRATION ONLY)
  // =====================================================
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation(
        "🚀 OutboxPublisherService started. Interval: {Interval}ms, MaxRetries: {MaxRetries}",
        _options.PollingInterval.TotalMilliseconds,
        _options.MaxRetries);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await ProcessOutboxOnceAsync(stoppingToken);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "🔥 Outbox loop iteration failed unexpectedly");
      }

      await Task.Delay(_options.PollingInterval, stoppingToken);
    }

    _logger.LogInformation("🛑 OutboxPublisherService stopped");
  }

  // =====================================================
  // DETERMINISTIC UNIT OF WORK (TESTABLE CORE)
  // =====================================================
  public async Task ProcessOutboxOnceAsync(CancellationToken ct)
  {
    var pending = await _messageStore.GetPendingAsync(ct);

    if (pending.Count == 0)
    {
      _logger.LogDebug("📭 No pending messages found in outbox");
      return;
    }

    foreach (var stored in pending)
    {
      if (ct.IsCancellationRequested)
        return;

      await ProcessSingleMessageAsync(stored, ct);
    }
  }

  // =====================================================
  // MESSAGE PROCESSING UNIT (ISOLATED FAILURE DOMAIN)
  // =====================================================
  private async Task ProcessSingleMessageAsync(StoredMessage stored, CancellationToken ct)
  {
    try
    {
      var message = stored.ToMessage();

      _logger.LogInformation(
          "📤 Sending message {MessageId} of type {MessageType} | Correlation: {CorrelationId}",
          stored.Id,
          stored.MessageType,
          stored.CorrelationId);

      await _sender.SendAsync(message, ct);

      await _messageStore.MarkAsSentAsync(stored.Id, ct);

      _logger.LogInformation("✅ Successfully sent message {MessageId}", stored.Id);
    }
    catch (Exception ex)
    {
      stored.RetryCount++;
      stored.LastError = ex.Message;
      stored.LastTriedOn = DateTime.UtcNow;

      if (stored.RetryCount >= _options.MaxRetries)
      {
        await _messageStore.MoveToDeadLetterAsync(stored, ct);

        _logger.LogError(
            ex,
            "💀 Message {MessageId} moved to DLQ after {Retries} retries.",
            stored.Id,
            stored.RetryCount);
      }
      else
      {
        await _messageStore.UpdateRetryAsync(stored, ct);

        _logger.LogWarning(
            ex,
            "⚠️ Message {MessageId} failed (Attempt {RetryCount}/{MaxCount}). Will retry.",
            stored.Id,
            stored.RetryCount,
            _options.MaxRetries);
      }
    }
  }
}