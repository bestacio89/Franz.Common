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

public sealed class OutboxMessageListener : IListener
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

  // ✅ FIX: Replaced synchronous event with awaitable Func delegate
  public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    _logger.LogInformation("🚀 OutboxMessageListener started polling...");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        // Fetching stored records
        var pending = await _messageStore.GetPendingAsync(stoppingToken);

        foreach (var stored in pending)
        {
          // Bridge stored data to our native Message
          var message = stored.ToMessage();

          // 👉 Inbox check (Idempotency)
          if (await _inbox.HasProcessedAsync(message.Id, stoppingToken))
          {
            _logger.LogInformation("⏩ Skipping already processed message {MessageId}", message.Id);
            await _messageStore.MarkAsSentAsync(stored.Id, stoppingToken);
            continue;
          }

          // 🚀 THE FIX: Await external processing before committing state changes
          if (OnMessageReceivedAsync != null)
          {
            await OnMessageReceivedAsync(new MessageEventArgs(message));
          }

          // Deserialize domain object for internal dispatch
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

          // 👉 Atomic logical completion: mark both stores
          await _inbox.MarkProcessedAsync(message.Id, stoppingToken);
          await _messageStore.MarkAsSentAsync(stored.Id, stoppingToken);

          _logger.LogInformation("✅ Marked Outbox message {MessageId} as sent + processed", message.Id);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "💥 Unexpected error in OutboxMessageListener loop");
      }

      // Polling interval - in a production scenario, consider a backoff strategy or SignalR/DbNotification trigger
      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }
  }

  public Task StopListenAsync(CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("🛑 OutboxMessageListener stopping...");
    return Task.CompletedTask;
  }

  private object? TryDeserializeDomainObject(Message message)
  {
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