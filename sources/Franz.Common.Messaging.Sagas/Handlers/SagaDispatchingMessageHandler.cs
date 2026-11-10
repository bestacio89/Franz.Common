#nullable enable

using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Messaging.Storage;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.Sagas.Handlers;

public sealed class SagaDispatchingMessageHandler : IMessageHandler
{
  private readonly SagaOrchestrator _orchestrator;
  private readonly IMessageSerializer _serializer;
  private readonly IInboxStore? _inboxStore;
  private readonly IUnitOfWork? _persistenceUnitOfWork;
  private readonly ITenantExecutionContextAccessor? _tenantExecutionContextAccessor;

  public SagaDispatchingMessageHandler(
      SagaOrchestrator orchestrator,
      IMessageSerializer serializer,
      IInboxStore? inboxStore = null)
      : this(orchestrator, serializer, inboxStore, null, null)
  {
  }

  public SagaDispatchingMessageHandler(
      SagaOrchestrator orchestrator,
      IMessageSerializer serializer,
      IInboxStore? inboxStore,
      IUnitOfWork? persistenceUnitOfWork)
      : this(orchestrator, serializer, inboxStore, persistenceUnitOfWork, null)
  {
  }

  public SagaDispatchingMessageHandler(
      SagaOrchestrator orchestrator,
      IMessageSerializer serializer,
      IInboxStore? inboxStore,
      IUnitOfWork? persistenceUnitOfWork,
      ITenantExecutionContextAccessor? tenantExecutionContextAccessor)
  {
    _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    _inboxStore = inboxStore;
    _persistenceUnitOfWork = persistenceUnitOfWork;
    _tenantExecutionContextAccessor = tenantExecutionContextAccessor;
  }

  public async Task ProcessAsync(Message message, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);

    var typeName = message.MessageType;

    // IntegrationEventMessageBuilderStrategy publishes this canonical header.
    // Read it before the legacy "type" fallback so standard Franz envelopes work.
    if (string.IsNullOrWhiteSpace(typeName) &&
        message.Headers.TryGetValue(MessagingConstants.ClassName, out var classNameValues) &&
        classNameValues.Length > 0)
      typeName = classNameValues[0];

    if (string.IsNullOrWhiteSpace(typeName) &&
        message.Headers.TryGetValue("type", out var typeValues) &&
        typeValues.Length > 0)
      typeName = typeValues[0];

    if (string.IsNullOrWhiteSpace(typeName))
      throw new PermanentMessagingException("Saga message does not declare a message type.");

    if (!_orchestrator.TryResolveMessageType(typeName, out var type) || type is null)
      throw new PermanentMessagingException(
          $"Saga message type '{typeName}' could not be resolved.");

    if (!typeof(IIntegrationEvent).IsAssignableFrom(type))
      throw new PermanentMessagingException(
          $"Saga message type '{typeName}' does not implement IIntegrationEvent.");

    if (string.IsNullOrWhiteSpace(message.Body))
      throw new PermanentMessagingException(
          $"Saga message '{message.Id}' has an empty body.");

    IIntegrationEvent integrationEvent;
    try
    {
      integrationEvent = (IIntegrationEvent?)_serializer.Deserialize(message.Body, type)
          ?? throw new PermanentMessagingException(
              $"Saga message '{message.Id}' deserialized to null.");
    }
    catch (PermanentMessagingException)
    {
      throw;
    }
    catch (Exception exception)
    {
      throw new PermanentMessagingException(
          $"Saga message '{message.Id}' could not be deserialized as '{typeName}'.",
          exception);
    }

    var correlationId = message.CorrelationId.GetValueOrDefault();
    if (correlationId == Guid.Empty)
      correlationId = Guid.CreateVersion7();

    Guid? causationId = message.Headers.TryGetValue(Message.CausationHeader, out var causationValues) &&
                      causationValues.Length > 0 &&
                      Guid.TryParse(causationValues[0], out var parsedCausationId)
        ? parsedCausationId
        : null;

    var tenantId = SagaTenantContextResolver.ResolveTenantId(message, _tenantExecutionContextAccessor);
    Guid? messageId = message.Id == Guid.Empty ? null : message.Id;
    var retrySagaId = SagaRetryHeaders.ReadTargetSagaId(message);

    if (retrySagaId.HasValue &&
        (!messageId.HasValue || message.GetProperty<bool>(MessagingConstants.TransportGeneratedMessageId)))
      throw new PermanentMessagingException(
          "Saga retry delivery requires a stable, non-empty message identity.");

    if (_inboxStore is not null)
    {
      if (!messageId.HasValue)
        throw new PermanentMessagingException(
            "Saga inbox processing requires a non-empty message identity.");

      if (message.GetProperty<bool>(MessagingConstants.TransportGeneratedMessageId))
        throw new PermanentMessagingException(
            "Saga inbox processing requires a stable broker-supplied message identity.");
    }

    if (_inboxStore is not null && _persistenceUnitOfWork is null)
      throw new InvalidOperationException(
          "Saga inbox processing requires a persistence IUnitOfWork so the receipt and saga writes can commit atomically.");

    var executionContext = MediatorContext.Current
        .WithCorrelationId(correlationId)
        .WithCausationId(causationId)
        .WithTenant(tenantId);

    using var contextScope = MediatorContext.BeginScope(executionContext);

    try
    {
      if (_persistenceUnitOfWork is not null)
        await _persistenceUnitOfWork.BeginAsync(ct).ConfigureAwait(false);

      if (_inboxStore is not null &&
          messageId.HasValue &&
          !await _inboxStore.TryRegisterAsync(messageId.Value, ct).ConfigureAwait(false))
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
          retrySagaId,
          ct).ConfigureAwait(false);

      if (_persistenceUnitOfWork is not null)
        await _persistenceUnitOfWork.CommitAsync(ct).ConfigureAwait(false);
    }
    catch (SagaFailedException)
    {
      // A Fail transition is a successful persistence outcome: commit the terminal
      // state and inbox receipt before surfacing the failure to the transport.
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
              "Saga message processing failed and its persistence transaction could not be rolled back.",
              operationException,
              rollbackException);
        }
      }

      throw;
    }
  }
}
