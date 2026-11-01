#nullable enable
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.MultiTenancy;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Listeners;

public sealed class OutboxMessageListener : IListener
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<OutboxMessageListener> _logger;

  public OutboxMessageListener(
      IServiceScopeFactory scopeFactory,
      ILogger<OutboxMessageListener> logger)
  {
    _scopeFactory = scopeFactory;
    _logger = logger;
  }

  public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    _logger.LogInformation("Outbox message listener started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        IReadOnlyList<StoredMessage> pending;

        await using (var pollingScope = _scopeFactory.CreateAsyncScope())
        {
          var messageStore = pollingScope.ServiceProvider.GetRequiredService<IMessageStore>();
          pending = await messageStore.GetPendingAsync(stoppingToken).ConfigureAwait(false);
        }

        foreach (var stored in pending)
        {
          stoppingToken.ThrowIfCancellationRequested();
          await ProcessMessageAsync(stored, stoppingToken).ConfigureAwait(false);
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Outbox polling or message processing failed.");
      }

      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
    }
  }

  private async Task ProcessMessageAsync(StoredMessage stored, CancellationToken cancellationToken)
  {
    await using var scope = _scopeFactory.CreateAsyncScope();
    var services = scope.ServiceProvider;
    var messageStore = services.GetRequiredService<IMessageStore>();
    var inbox = services.GetRequiredService<IInboxStore>();
    var dispatcher = services.GetRequiredService<IDispatcher>();
    var serializer = services.GetRequiredService<IMessageSerializer>();
    var message = stored.ToMessage();
    var messageContextAccessor = services.GetService<MessageContextAccessor>();

    try
    {
      if (await inbox.HasProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false))
      {
        _logger.LogInformation("Skipping already processed message {MessageId}.", message.Id);
        await messageStore.MarkAsSentAsync(stored.Id, cancellationToken).ConfigureAwait(false);
        return;
      }

      var tenantInitializer = services.GetService<ITenantMessageContextInitializer>();
      await using var tenantExecutionScope = tenantInitializer is null
          ? null
          : await tenantInitializer.BeginAsync(message, cancellationToken).ConfigureAwait(false);

      if (OnMessageReceivedAsync is not null)
        await OnMessageReceivedAsync(new MessageEventArgs(message)).ConfigureAwait(false);

      // A nested hosted handler may clear the ambient AsyncLocal; restore it for this dispatch scope.
      messageContextAccessor?.Set(new MessageContext(message));
      var domainObject = TryDeserializeDomainObject(message, serializer);

      using var logScope = _logger.BeginScope(new Dictionary<string, object?>
      {
        ["FranzCorrelationId"] = message.CorrelationId ?? Guid.Empty,
        ["FranzMessageId"] = message.Id
      });

      if (domainObject is IEvent evt)
      {
        _logger.LogInformation("Dispatching outbox event {EventType}.", evt.GetType().Name);
        await dispatcher.PublishEventAsync(evt, cancellationToken).ConfigureAwait(false);
      }
      else if (domainObject is ICommand cmd)
      {
        _logger.LogInformation("Dispatching outbox command {CommandType}.", cmd.GetType().Name);
        await dispatcher.SendAsync(cmd, cancellationToken).ConfigureAwait(false);
      }
      else
      {
        _logger.LogWarning("Outbox message {MessageId} is neither an ICommand nor an IEvent.", message.Id);
      }

      await inbox.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
      await messageStore.MarkAsSentAsync(stored.Id, cancellationToken).ConfigureAwait(false);

      _logger.LogInformation("Marked outbox message {MessageId} as sent and processed.", message.Id);
    }
    finally
    {
      messageContextAccessor?.Clear();
    }
  }

  public Task StopListenAsync(CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Outbox message listener stopping.");
    return Task.CompletedTask;
  }

  private object? TryDeserializeDomainObject(Message message, IMessageSerializer serializer)
  {
    var typeName = message.MessageType;

    if (string.IsNullOrWhiteSpace(typeName) || message.Body is not string bodyJson)
      return null;

    var type = ResolveType(typeName);
    if (type is null)
      return null;

    try
    {
      return serializer.Deserialize(bodyJson, type);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to deserialize outbox message {MessageId} as {TypeName}.",
          message.Id, typeName);
      return null;
    }
  }

  private static Type? ResolveType(string typeName)
  {
    var type = Type.GetType(typeName);
    if (type is not null)
      return type;

    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      type = assembly.GetType(typeName);
      if (type is not null)
        return type;
    }

    return null;
  }
}
