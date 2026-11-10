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
using Franz.Common.Messaging.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Hosting.Listeners;

public sealed class OutboxMessageListener : IListener
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<OutboxMessageListener> _logger;
  private readonly int _batchSize;
  private readonly TimeSpan _leaseDuration;

  /// <summary>
  /// Creates a listener with the default polling batch size for binary compatibility.
  /// </summary>
  public OutboxMessageListener(
      IServiceScopeFactory scopeFactory,
      ILogger<OutboxMessageListener> logger)
      : this(scopeFactory, logger, Options.Create(new OutboxOptions()))
  {
  }

  /// <summary>
  /// Creates a listener using the configured outbox polling batch size.
  /// </summary>
  public OutboxMessageListener(
      IServiceScopeFactory scopeFactory,
      ILogger<OutboxMessageListener> logger,
      IOptions<OutboxOptions> options)
  {
    ArgumentNullException.ThrowIfNull(scopeFactory);
    ArgumentNullException.ThrowIfNull(logger);
    ArgumentNullException.ThrowIfNull(options);

    _scopeFactory = scopeFactory;
    _logger = logger;
    _batchSize = options.Value.BatchSize;
    _leaseDuration = options.Value.LeaseDuration;
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_batchSize);
    if (_leaseDuration <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options), "Outbox LeaseDuration must be greater than zero.");
  }

  public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    using (var startupScope = _scopeFactory.CreateScope())
      TenantMessagingStartupValidation.Validate(startupScope.ServiceProvider);

    _logger.LogInformation("Outbox message listener started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        for (var index = 0; index < _batchSize; index++)
        {
          stoppingToken.ThrowIfCancellationRequested();
          var leaseOwnerId = Guid.CreateVersion7();
          IReadOnlyList<StoredMessage> claimed;

          await using (var pollingScope = _scopeFactory.CreateAsyncScope())
          {
            var messageStore = pollingScope.ServiceProvider.GetRequiredService<IMessageStore>();
            claimed = await messageStore.ClaimPendingAsync(
                leaseOwnerId, _leaseDuration, 1, stoppingToken).ConfigureAwait(false);
          }

          if (claimed.Count == 0)
            break;

          await ProcessMessageAsync(claimed[0], leaseOwnerId, stoppingToken).ConfigureAwait(false);
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

  private async Task ProcessMessageAsync(StoredMessage stored, Guid leaseOwnerId, CancellationToken cancellationToken)
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
        await messageStore.MarkAsSentAsync(stored.Id, leaseOwnerId, cancellationToken).ConfigureAwait(false);
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
      var domainObject = TryDeserializeDomainObject(message, serializer)
          ?? throw new InvalidOperationException(
              $"Outbox message '{message.Id}' could not be resolved to a command or event.");

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
        throw new InvalidOperationException(
            $"Outbox message '{message.Id}' resolved to unsupported type '{domainObject.GetType().FullName}'.");
      }

      await inbox.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
      await messageStore.MarkAsSentAsync(stored.Id, leaseOwnerId, cancellationToken).ConfigureAwait(false);

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
    ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

    // Prefer the CLR resolver. It understands assembly-qualified names,
    // nested types, and generic type expressions.
    var type = Type.GetType(
        typeName,
        throwOnError: false,
        ignoreCase: false);

    if (type is not null)
      return type;

    // Assembly.GetType expects the type's full name, not its
    // assembly-qualified name. Find the top-level separator while
    // preserving commas inside generic type argument expressions.
    var separatorIndex = FindAssemblySeparator(typeName);
    var fullTypeName = separatorIndex < 0
        ? typeName.Trim()
        : typeName[..separatorIndex].Trim();

    if (fullTypeName.Length == 0)
      return null;

    if (separatorIndex >= 0)
    {
      var assemblyIdentity = typeName[(separatorIndex + 1)..].Trim();
      var assemblyName = assemblyIdentity.Split(',', 2)[0].Trim();

      if (assemblyName.Length > 0)
      {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
          if (!string.Equals(
                  assembly.GetName().Name,
                  assemblyName,
                  StringComparison.OrdinalIgnoreCase))
            continue;

          type = assembly.GetType(
              fullTypeName,
              throwOnError: false,
              ignoreCase: false);

          if (type is not null)
            return type;
        }
      }
    }

    // Last resort: resolve an unqualified type name against loaded assemblies.
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      type = assembly.GetType(
          fullTypeName,
          throwOnError: false,
          ignoreCase: false);

      if (type is not null)
        return type;
    }

    return null;
  }

  private static int FindAssemblySeparator(string typeName)
  {
    var genericDepth = 0;

    for (var index = 0; index < typeName.Length; index++)
    {
      switch (typeName[index])
      {
        case '[':
          genericDepth++;
          break;

        case ']':
          genericDepth--;

          if (genericDepth < 0)
            return -1;

          break;

        case ',' when genericDepth == 0:
          return index;
      }
    }

    return -1;
  }
}
