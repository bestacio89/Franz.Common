#nullable enable
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Listeners;

public class OutboxMessageListener : IListener
{
  private readonly IMessageStore _messageStore;
  private readonly IInboxStore _inbox;
  private readonly IDispatcher _dispatcher;
  private readonly IMessageSerializer _serializer;
  private readonly ILogger<OutboxMessageListener> _logger;

  public OutboxMessageListener(
      IMessageStore messageStore,
      IInboxStore inbox,
      IDispatcher dispatcher,
      IMessageSerializer serializer,
      ILogger<OutboxMessageListener> logger)
  {
    _messageStore = messageStore;
    _inbox = inbox;
    _dispatcher = dispatcher;
    _serializer = serializer;
    _logger = logger;
  }

  public event EventHandler<MessageEventArgs>? Received;

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        // Fetching stored records (StoredMessage.Id should be Guid)
        var pending = await _messageStore.GetPendingAsync(stoppingToken);

        foreach (var stored in pending)
        {
          // Bridge stored data to our native Guid v7 Message
          var message = stored.ToMessage();

          // 👉 Inbox check (High-performance Guid comparison)
          if (await _inbox.HasProcessedAsync(message.Id, stoppingToken))
          {
            _logger.LogInformation("⏩ Skipping already processed message {MessageId}", message.Id);
            // Mark as sent in outbox so we don't fetch it again, even if it was a duplicate
            await _messageStore.MarkAsSentAsync(stored.Id, stoppingToken);
            continue;
          }

          // Fire event for subscribers
          Received?.Invoke(this, new MessageEventArgs(message));

          // Deserialize domain object
          object? domainObject = TryDeserializeDomainObject(message);

          using var scope = _logger.BeginScope(new Dictionary<string, object>
          {
            ["FranzCorrelationId"] = message.CorrelationId,
            ["FranzMessageId"] = message.Id
          });

          if (domainObject is IEvent evt)
          {
            _logger.LogInformation("📢 Dispatching event {EventType} from Outbox", evt.GetType().Name);
            await _dispatcher.PublishEventAsync(evt, stoppingToken);
          }
          else if (domainObject is ICommand cmd)
          {
            _logger.LogInformation("📨 Dispatching command {CommandType} from Outbox", cmd.GetType().Name);
            await _dispatcher.SendAsync(cmd, stoppingToken);
          }
          else
          {
            _logger.LogWarning("⚠️ Outbox message {Id} is neither ICommand nor IEvent", message.Id);
          }

          // 👉 Atomic logical completion
          await _inbox.MarkProcessedAsync(message.Id, stoppingToken);
          await _messageStore.MarkAsSentAsync(stored.Id, stoppingToken);

          _logger.LogInformation("✅ Marked Outbox message {MessageId} as sent + processed", message.Id);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "💥 Unexpected error in OutboxMessageListener loop");
      }

      // Consider making this interval configurable
      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }
  }

  public void StopListen() { /* No-op */ }

  private object? TryDeserializeDomainObject(Message message)
  {
    // Use the MessageType property we defined in the Message base class
    var typeName = message.MessageType;

    if (!string.IsNullOrWhiteSpace(typeName) && message.Body is string bodyJson)
    {
      var type = ResolveType(typeName);
      if (type != null)
      {
        try
        {
          return _serializer.Deserialize(bodyJson, type);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "❌ Failed to deserialize message {Id} as {TypeName}", message.Id, typeName);
        }
      }
    }
    return null;
  }

  private static Type? ResolveType(string typeName)
  {
    var type = Type.GetType(typeName);
    if (type != null) return type;

    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    {
      type = asm.GetType(typeName);
      if (type != null) return type;
    }
    return null;
  }
}