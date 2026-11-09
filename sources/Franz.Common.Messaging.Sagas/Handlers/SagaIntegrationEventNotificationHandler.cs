#nullable enable

using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Storage;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.Sagas.Handlers;

/// <summary>
/// Connects integration-event notifications from the inbound Messaging pipeline
/// to the Saga orchestrator while preserving the original message execution boundary.
/// </summary>
internal sealed class SagaIntegrationEventNotificationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
  private readonly SagaOrchestrator _orchestrator;
  private readonly IMessageContextAccessor? _messageContextAccessor;
  private readonly ITenantExecutionContextAccessor? _tenantExecutionContextAccessor;
  private readonly IInboxStore? _inboxStore;
  private readonly IUnitOfWork? _persistenceUnitOfWork;

  public SagaIntegrationEventNotificationHandler(
      SagaOrchestrator orchestrator,
      IMessageContextAccessor? messageContextAccessor = null,
      ITenantExecutionContextAccessor? tenantExecutionContextAccessor = null,
      IInboxStore? inboxStore = null,
      IUnitOfWork? persistenceUnitOfWork = null)
  {
    _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    _messageContextAccessor = messageContextAccessor;
    _tenantExecutionContextAccessor = tenantExecutionContextAccessor;
    _inboxStore = inboxStore;
    _persistenceUnitOfWork = persistenceUnitOfWork;
  }

  public async Task Handle(TNotification notification, CancellationToken cancellationToken = default)
  {
    if (notification is not IIntegrationEvent integrationEvent)
      return;

    var message = _messageContextAccessor?.Current?.Message;
    var correlationId = message?.CorrelationId ?? MediatorContext.EnsureCorrelationId();
    if (correlationId == Guid.Empty)
      correlationId = MediatorContext.EnsureCorrelationId();

    var causationId = ResolveCausationId(message);
    Guid? messageId = message is { Id: var id } && id != Guid.Empty ? id : null;
    var tenantId = SagaTenantContextResolver.ResolveTenantId(message, _tenantExecutionContextAccessor);

    if (_inboxStore is not null)
    {
      if (message is null)
        throw new PermanentMessagingException(
            "Saga inbox processing requires the original inbound message context.");

      if (!messageId.HasValue)
        throw new PermanentMessagingException(
            "Saga inbox processing requires a non-empty message identity.");

      if (message.GetProperty<bool>(MessagingConstants.TransportGeneratedMessageId))
        throw new PermanentMessagingException(
            "Saga inbox processing requires a stable broker-supplied message identity.");
    }

    var executionContext = MediatorContext.Current
        .WithCorrelationId(correlationId)
        .WithCausationId(causationId)
        .WithTenant(tenantId);
    using var contextScope = MediatorContext.BeginScope(executionContext);

    if (_inboxStore is not null && _persistenceUnitOfWork is null)
      throw new InvalidOperationException(
          "Saga inbox processing requires a persistence IUnitOfWork so the receipt and saga writes can commit atomically.");

    try
    {
      if (_persistenceUnitOfWork is not null)
        await _persistenceUnitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);

      if (_inboxStore is not null &&
          messageId.HasValue &&
          !await _inboxStore.TryRegisterAsync(messageId.Value, cancellationToken).ConfigureAwait(false))
      {
        await _persistenceUnitOfWork!.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        return;
      }

      await _orchestrator.HandleEventAsync(
          integrationEvent,
          correlationId,
          causationId,
          messageId,
          tenantId,
          cancellationToken).ConfigureAwait(false);

      if (_persistenceUnitOfWork is not null)
        await _persistenceUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (SagaFailedException)
    {
      // A terminal Fail transition is persisted before its exception is propagated
      // to the transport for its configured failure disposition.
      if (_persistenceUnitOfWork is not null)
        await _persistenceUnitOfWork.CommitAsync(CancellationToken.None).ConfigureAwait(false);

      throw;
    }
    catch (Exception operationException)
    {
      if (_persistenceUnitOfWork is not null)
      {
        try
        {
          await _persistenceUnitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception rollbackException)
        {
          throw new AggregateException(
              "Saga notification processing failed and its persistence transaction could not be rolled back.",
              operationException,
              rollbackException);
        }
      }

      throw;
    }
  }

  private static Guid? ResolveCausationId(Message? message)
  {
    if (message is not null &&
        message.Headers.TryGetValue(Message.CausationHeader, out var values) &&
        values.Length == 1 &&
        Guid.TryParse(values[0], out var causationId))
      return causationId;

    return MediatorContext.Current.CausationId;
  }
}
