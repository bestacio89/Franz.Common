#nullable enable

using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Headers;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Storage;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Messages;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Franz.Common.Serialization;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Executes saga handlers and persists their lifecycle transitions.
/// </summary>
public sealed class SagaOrchestrator
{
  private readonly SagaRouter _router;
  private readonly ISagaRepository _repository;
  private readonly SagaExecutionPipeline _pipeline;
  private readonly IMessagingPublisher _publisher;
  private readonly IMessageStore? _messageStore;
  private readonly IUnitOfWork? _persistenceUnitOfWork;
  private readonly IServiceProvider _services;
  private readonly IReadOnlyCollection<ISagaAuditSink> _auditSinks;
  private readonly FranzSagaOptions _options;
  private readonly ILogger<SagaOrchestrator> _logger;

  public SagaOrchestrator(
      SagaRouter router,
      ISagaRepository repository,
      SagaExecutionPipeline pipeline,
      IMessagingPublisher publisher,
      IServiceProvider services)
      : this(router, repository, pipeline, publisher, services,
          Array.Empty<ISagaAuditSink>(), new FranzSagaOptions(), NullLogger<SagaOrchestrator>.Instance, null, null)
  {
  }

  public SagaOrchestrator(
      SagaRouter router,
      ISagaRepository repository,
      SagaExecutionPipeline pipeline,
      IMessagingPublisher publisher,
      IServiceProvider services,
      IEnumerable<ISagaAuditSink> auditSinks,
      FranzSagaOptions options,
      ILogger<SagaOrchestrator>? logger = null)
      : this(router, repository, pipeline, publisher, services, auditSinks, options, logger, null, null)
  {
  }

  public SagaOrchestrator(
      SagaRouter router,
      ISagaRepository repository,
      SagaExecutionPipeline pipeline,
      IMessagingPublisher publisher,
      IServiceProvider services,
      IEnumerable<ISagaAuditSink> auditSinks,
      FranzSagaOptions options,
      ILogger<SagaOrchestrator>? logger,
      IMessageStore? messageStore,
      IUnitOfWork? persistenceUnitOfWork = null)
  {
    _router = router ?? throw new ArgumentNullException(nameof(router));
    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    _services = services ?? throw new ArgumentNullException(nameof(services));
    _auditSinks = (auditSinks ?? throw new ArgumentNullException(nameof(auditSinks))).ToArray();
    _options = options ?? throw new ArgumentNullException(nameof(options));
    _logger = logger ?? NullLogger<SagaOrchestrator>.Instance;
    _messageStore = messageStore;
    _persistenceUnitOfWork = persistenceUnitOfWork;
  }

  internal bool TryResolveMessageType(string typeName, out Type? messageType)
      => _router.TryResolveMessageType(typeName, out messageType);

  public Task HandleEventAsync(
      IIntegrationEvent incomingEvent,
      Guid correlationId,
      Guid? causationId,
      CancellationToken cancellationToken = default)
      => HandleEventAsync(
          incomingEvent,
          correlationId,
          causationId,
          messageId: null,
          tenantId: null,
          retrySagaId: null,
          originalMessage: null,
          cancellationToken);

  public Task HandleEventAsync(
      IIntegrationEvent incomingEvent,
      Guid correlationId,
      Guid? causationId,
      Guid? messageId,
      string? tenantId,
      CancellationToken cancellationToken = default)
      => HandleEventAsync(
          incomingEvent,
          correlationId,
          causationId,
          messageId,
          tenantId,
          retrySagaId: null,
          originalMessage: null,
          cancellationToken);

  public Task HandleEventAsync(
      IIntegrationEvent incomingEvent,
      Guid correlationId,
      Guid? causationId,
      Guid? messageId,
      string? tenantId,
      Guid? retrySagaId,
      CancellationToken cancellationToken = default)
      => HandleEventAsync(
          incomingEvent,
          correlationId,
          causationId,
          messageId,
          tenantId,
          retrySagaId,
          originalMessage: null,
          cancellationToken);

  public async Task HandleEventAsync(
      IIntegrationEvent incomingEvent,
      Guid correlationId,
      Guid? causationId,
      Guid? messageId,
      string? tenantId,
      Guid? retrySagaId,
      Message? originalMessage,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(incomingEvent);
    cancellationToken.ThrowIfCancellationRequested();

    if (retrySagaId == Guid.Empty)
      throw new PermanentMessagingException("Saga retry target identity cannot be empty.");

    var messageType = incomingEvent.GetType();
    var retryMatched = false;

    SagaFailedException? failure = null;
    foreach (var registration in _router.ResolveRegistrationsForMessage(messageType))
    {
      try
      {
        if (retrySagaId.HasValue)
        {
          retryMatched |= await ExecuteRetryForMessageAsync(
              incomingEvent,
              registration,
              retrySagaId.Value,
              correlationId,
              causationId,
              messageId,
              tenantId,
              originalMessage,
              cancellationToken).ConfigureAwait(false);
        }
        else
        {
          await ExecuteSagaForMessageAsync(
              incomingEvent,
              registration,
              correlationId,
              causationId,
              messageId,
              tenantId,
              originalMessage,
              cancellationToken).ConfigureAwait(false);
        }
      }
      catch (SagaFailedException exception)
      {
        // A terminal failure is a persisted lifecycle transition. Continue routing
        // the same incoming message to every matching saga before the unit commits.
        failure ??= exception;
      }
    }

    if (failure is not null)
      ExceptionDispatchInfo.Capture(failure).Throw();

    if (retrySagaId.HasValue && !retryMatched)
      throw new PermanentMessagingException(
          $"Saga retry delivery does not match a pending retry for saga '{retrySagaId.Value}'.");
  }

  private async Task<bool> ExecuteRetryForMessageAsync(
      IIntegrationEvent message,
      SagaRegistration registration,
      Guid retrySagaId,
      Guid correlationId,
      Guid? causationId,
      Guid? messageId,
      string? tenantId,
      Message? originalMessage,
      CancellationToken cancellationToken)
  {
    if (!messageId.HasValue || messageId.Value == Guid.Empty)
      throw new PermanentMessagingException(
          "Saga retry delivery requires a stable, non-empty message identity.");

    var record = await _repository.LoadAsync(
        retrySagaId,
        tenantId,
        typeof(object),
        cancellationToken).ConfigureAwait(false);

    if (record is null)
      return false;

    var expectedSagaType = registration.SagaType.AssemblyQualifiedName ?? registration.SagaType.FullName!;
    if (!string.Equals(record.SagaType, expectedSagaType, StringComparison.Ordinal))
      return false;

    if (record.Status != SagaLifecycleStatus.RetryScheduled ||
        record.LastMessageId != messageId ||
        record.RetryCount <= 0 ||
        record.NextAttemptAt is null)
      throw new PermanentMessagingException(
          $"Message '{messageId.Value}' is not the persisted retry delivery for saga '{retrySagaId}'.");

    if (record.NextAttemptAt.Value > DateTimeOffset.UtcNow)
      throw new PermanentMessagingException(
          $"Saga retry delivery for saga '{retrySagaId}' is not due until '{record.NextAttemptAt.Value:O}'.");

    if (record.State is not ISagaState state ||
        !registration.StateType.IsInstanceOfType(state))
      throw new SagaConfigurationError(
          $"Persisted state for saga '{retrySagaId}' is incompatible with '{registration.SagaType.FullName}'.");

    var saga = _services.GetService(registration.SagaType)
        ?? throw new SagaConfigurationError(
            $"Saga '{registration.SagaType.FullName}' is not registered in the service provider.");

    MethodInfo? handler;
    if (registration.CanStartWith(message.GetType()))
      handler = registration.StartHandlers.GetValueOrDefault(message.GetType());
    else if (registration.StepHandlers.TryGetValue(message.GetType(), out var stepHandler))
      handler = stepHandler;
    else
      handler = registration.CompensationHandlers.GetValueOrDefault(message.GetType());

    if (handler is null)
      throw new SagaConfigurationError(
          $"Saga '{registration.SagaType.Name}' has no handler for retry message '{message.GetType().Name}'.");

    SetSagaId(saga, retrySagaId);
    SetSagaState(saga, registration, state);

    var effectiveCorrelationId = ResolveCorrelationId(record, correlationId);
    var resumeStatus = registration.CanCompensate(message.GetType())
        ? SagaLifecycleStatus.Compensating
        : SagaLifecycleStatus.Running;

    var executionContext = new SagaContext(
        retrySagaId,
        registration.SagaType,
        state,
        message,
        effectiveCorrelationId,
        causationId,
        cancellationToken);

    var handlerTimer = Stopwatch.StartNew();
    var result = await ExecuteHandlerAsync(
        saga,
        message,
        handler,
        executionContext,
        cancellationToken).ConfigureAwait(false);
    handlerTimer.Stop();

    EnsureSagaIdentityUnchanged(saga, retrySagaId);

    await PersistTransitionAsync(
        record with
        {
          Status = resumeStatus,
          CorrelationId = effectiveCorrelationId,
          State = GetSagaState(saga, registration),
          LastMessageId = messageId
        },
        record.Version,
        result,
        message,
        originalMessage,
        messageId,
        message.GetType(),
        registration.CanCompensate(message.GetType()) ? "Compensate" : "Retry",
        effectiveCorrelationId,
        causationId,
        tenantId,
        handlerTimer.Elapsed,
        cancellationToken).ConfigureAwait(false);

    return true;
  }

  private async Task ExecuteSagaForMessageAsync(
      IIntegrationEvent message,
      SagaRegistration registration,
      Guid correlationId,
      Guid? causationId,
      Guid? messageId,
      string? tenantId,
      Message? originalMessage,
      CancellationToken cancellationToken)
  {
    var messageType = message.GetType();
    var saga = _services.GetService(registration.SagaType)
        ?? throw new SagaConfigurationError(
            $"Saga '{registration.SagaType.FullName}' is not registered in the service provider.");

    if (registration.CanStartWith(messageType))
    {
      var effectiveCorrelationId = correlationId == Guid.Empty ? Guid.CreateVersion7() : correlationId;
      var state = Activator.CreateInstance(registration.StateType) as ISagaState
          ?? throw new SagaConfigurationError(
              $"Could not create saga state '{registration.StateType.FullName}'.");

      SetSagaState(saga, registration, state);

      // A start contract may opt into deterministic business identity. Without a
      // resolver, SagaBase's generated Guid remains the backwards-compatible default.
      var startResolverType = typeof(ISagaIdResolver<>).MakeGenericType(messageType);
      var sagaId = startResolverType.IsInstanceOfType(saga)
          ? ResolveSagaId(saga, message, registration)
          : GetSagaId(saga);

      if (GetSagaId(saga) != sagaId)
        SetSagaId(saga, sagaId);

      // Reject an already-persisted instance before invoking application handlers.
      // The repository's unique tenant-scoped create remains the concurrency arbiter
      // when two start deliveries race after this preflight check.
      var existingStart = await _repository.LoadAsync(
          sagaId,
          tenantId,
          registration.StateType,
          cancellationToken).ConfigureAwait(false);

      if (existingStart is not null)
        throw new SagaConcurrencyException(sagaId, expectedVersion: null);

      var context = new SagaContext(
          sagaId,
          registration.SagaType,
          state,
          message,
          effectiveCorrelationId,
          causationId,
          cancellationToken);

      await CallOnCreatedAsync(saga, context, cancellationToken).ConfigureAwait(false);

      var startHandlerTimer = Stopwatch.StartNew();
      var transition = await ExecuteHandlerAsync(
          saga,
          message,
          registration.StartHandlers[messageType],
          context,
          cancellationToken).ConfigureAwait(false);
      startHandlerTimer.Stop();

      EnsureSagaIdentityUnchanged(saga, sagaId);

      var now = DateTimeOffset.UtcNow;
      var record = new SagaPersistenceRecord
      {
        SagaId = sagaId,
        CorrelationId = effectiveCorrelationId,
        TenantId = tenantId,
        SagaType = registration.SagaType.AssemblyQualifiedName ?? registration.SagaType.FullName!,
        State = GetSagaState(saga, registration),
        Status = SagaLifecycleStatus.Running,
        CreatedAt = now,
        UpdatedAt = now,
        LastMessageId = messageId
      };

      await PersistTransitionAsync(
          record,
          expectedVersion: null,
          transition,
          message,
          originalMessage,
          messageId,
          messageType,
          "Start",
          effectiveCorrelationId,
          causationId,
          tenantId,
          startHandlerTimer.Elapsed,
          cancellationToken).ConfigureAwait(false);

      return;
    }

    var sagaIdExisting = ResolveSagaId(saga, message, registration);
    SetSagaId(saga, sagaIdExisting);

    var existingRecord = await _repository.LoadAsync(
        sagaIdExisting,
        tenantId,
        registration.StateType,
        cancellationToken).ConfigureAwait(false)
        ?? throw new SagaNotFoundException(
            $"Saga '{registration.SagaType.Name}' with ID '{sagaIdExisting}' was not found.");

    var expectedSagaType = registration.SagaType.AssemblyQualifiedName ?? registration.SagaType.FullName!;
    if (!string.Equals(existingRecord.SagaType, expectedSagaType, StringComparison.Ordinal))
      throw new SagaConfigurationError(
          $"Saga ID '{sagaIdExisting}' is persisted for '{existingRecord.SagaType}', not '{expectedSagaType}'.");

    var effectiveExistingCorrelationId = ResolveCorrelationId(existingRecord, correlationId);

    switch (existingRecord.Status)
    {
      case SagaLifecycleStatus.Running:
        if (!registration.CanHandle(messageType) && registration.CanCompensate(messageType))
          throw new SagaLifecycleException(sagaIdExisting, existingRecord.Status);
        break;

      case SagaLifecycleStatus.Compensating:
        if (!registration.CanCompensate(messageType))
          throw new SagaLifecycleException(sagaIdExisting, existingRecord.Status);
        break;

      case SagaLifecycleStatus.RetryScheduled:
      case SagaLifecycleStatus.Completed:
      case SagaLifecycleStatus.Failed:
      default:
        throw new SagaLifecycleException(sagaIdExisting, existingRecord.Status);
    }

    SetSagaState(saga, registration, (ISagaState)existingRecord.State);

    var executionContext = new SagaContext(
        sagaIdExisting,
        registration.SagaType,
        (ISagaState)existingRecord.State,
        message,
        effectiveExistingCorrelationId,
        causationId,
        cancellationToken);

    var handler = registration.StepHandlers.TryGetValue(messageType, out var stepHandler)
        ? stepHandler
        : registration.CompensationHandlers.GetValueOrDefault(messageType);

    if (handler is null)
      throw new SagaConfigurationError(
          $"Saga '{registration.SagaType.Name}' was routed message '{messageType.Name}' " +
          "but no step or compensation handler was registered.");

    var handlerTimer = Stopwatch.StartNew();
    var result = await ExecuteHandlerAsync(
        saga,
        message,
        handler,
        executionContext,
        cancellationToken).ConfigureAwait(false);
    handlerTimer.Stop();

    EnsureSagaIdentityUnchanged(saga, sagaIdExisting);

    await PersistTransitionAsync(
        existingRecord with
        {
          CorrelationId = effectiveExistingCorrelationId,
          State = GetSagaState(saga, registration),
          LastMessageId = messageId
        },
        existingRecord.Version,
        result,
        message,
        originalMessage,
        messageId,
        messageType,
        registration.CanCompensate(messageType) ? "Compensate" : "Step",
        effectiveExistingCorrelationId,
        causationId,
        tenantId,
        handlerTimer.Elapsed,
        cancellationToken).ConfigureAwait(false);
  }

  private async Task PersistTransitionAsync(
      SagaPersistenceRecord record,
      long? expectedVersion,
      ISagaTransition transition,
      IIntegrationEvent incomingEvent,
      Message? originalMessage,
      Guid? messageId,
      Type incomingMessageType,
      string stepType,
      Guid correlationId,
      Guid? causationId,
      string? tenantId,
      TimeSpan duration,
      CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(transition);

    if (transition.OutgoingMessage is not null &&
        transition.OutgoingMessage is not IIntegrationEvent)
      throw new SagaConfigurationError(
          $"Saga '{record.SagaType}' produced outgoing message '{transition.OutgoingMessage.GetType().FullName}'. " +
          "Only IIntegrationEvent messages can be published by the saga orchestrator.");

    var retryLimitExceeded = false;
    if (transition.Type == SagaTransitionType.Retry)
    {
      if (transition.OutgoingMessage is not null)
        throw new SagaConfigurationError("A retry transition cannot publish a separate outgoing message.");

      if (_options.Persistence.Equals("Memory", StringComparison.OrdinalIgnoreCase) ||
          _messageStore is null ||
          _persistenceUnitOfWork is null)
        throw new SagaConfigurationError(
            "Durable saga retry requires EF Core, MongoDB, or Cosmos persistence with its transactional IUnitOfWork and scheduled IMessageStore.");

      if (!messageId.HasValue || messageId.Value == Guid.Empty ||
          originalMessage is null ||
          originalMessage.GetProperty<bool>(MessagingConstants.TransportGeneratedMessageId))
        throw new SagaConfigurationError(
            "Durable saga retry requires the original inbound message to have a stable, broker-supplied identity.");

      if (_options.MaxRetryAttempts < 1)
        throw new SagaConfigurationError("Saga MaxRetryAttempts must be greater than zero.");

      if (record.RetryCount >= _options.MaxRetryAttempts)
      {
        transition = SagaTransition.Fail(
            new SagaRetryLimitExceededException(
                record.SagaId,
                _options.MaxRetryAttempts,
                transition.Error));
        retryLimitExceeded = true;
      }
      else
      {
        var delay = transition.Delay ?? _options.DefaultRetryDelay;
        if (delay <= TimeSpan.Zero)
          throw new SagaConfigurationError("Saga retry delay must be greater than zero.");

        if (originalMessage is null ||
            originalMessage.Id != messageId.Value ||
            string.IsNullOrWhiteSpace(originalMessage.Body))
          throw new SagaConfigurationError(
              "Durable saga retry requires the original inbound message envelope and its non-empty body.");

        var scheduledAt = DateTimeOffset.UtcNow.Add(delay);
        var retryHeaders = originalMessage.Headers.ToDictionary(
            header => header.Key,
            header => header.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
        retryHeaders.Remove(SagaRetryHeaders.TargetSagaId);
        retryHeaders[Message.CorrelationHeader] = [correlationId.ToString("D")];
        retryHeaders[Message.CausationHeader] = [messageId.Value.ToString("D")];
        if (!string.IsNullOrWhiteSpace(tenantId))
          retryHeaders[HeaderConstants.TenantId] = [tenantId];
        else
          retryHeaders.Remove(HeaderConstants.TenantId);

        var retryProperties = originalMessage.Properties.ToDictionary(
            property => property.Key,
            property => JsonSerializer.Serialize(
                property.Value,
                property.Value?.GetType() ?? typeof(object),
                FranzJson.Default),
            StringComparer.OrdinalIgnoreCase);
        retryProperties.Remove(MessagingConstants.TransportGeneratedMessageId);

        var retryRequest = new SagaRetryRequested
        {
          SagaId = record.SagaId,
          OriginalMessageId = originalMessage.Id,
          OriginalMessageType = incomingEvent.GetType().AssemblyQualifiedName
              ?? incomingEvent.GetType().FullName
              ?? incomingEvent.GetType().Name,
          Body = originalMessage.Body!,
          Headers = retryHeaders,
          Properties = retryProperties,
          OccurredOn = originalMessage.OccurredOn,
          AggregateId = originalMessage.AggregateId,
          Kind = originalMessage.Kind
        };

        var retryMessage = new IntegrationEventMessageBuilderStrategy().Build(retryRequest);
        retryMessage.MessageType = typeof(SagaRetryRequested).AssemblyQualifiedName
            ?? typeof(SagaRetryRequested).FullName!;
        retryMessage.CorrelationId = correlationId;
        retryMessage.Headers[Message.CausationHeader] = [messageId.Value.ToString("D")];
        if (!string.IsNullOrWhiteSpace(tenantId))
          retryMessage.Headers[HeaderConstants.TenantId] = [tenantId];
        retryMessage.Headers[SagaRetryHeaders.TargetSagaId] = [record.SagaId.ToString("D")];

        var scheduledRecord = record with
        {
          Status = SagaLifecycleStatus.RetryScheduled,
          UpdatedAt = DateTimeOffset.UtcNow,
          NextAttemptAt = scheduledAt,
          RetryCount = checked(record.RetryCount + 1),
          LastMessageId = retryMessage.Id,
          LastError = transition.Error?.Message ?? record.LastError
        };

        await _repository.SaveAsync(scheduledRecord, expectedVersion, cancellationToken)
            .ConfigureAwait(false);

        await WriteAuditAsync(
            scheduledRecord,
            transition,
            incomingMessageType,
            stepType,
            correlationId,
            causationId,
            messageId,
            tenantId,
            duration,
            cancellationToken).ConfigureAwait(false);

        await _messageStore.SaveScheduledAsync(retryMessage, scheduledAt, cancellationToken)
            .ConfigureAwait(false);

        return;
      }
    }

    if (transition.Type == SagaTransitionType.Compensate && transition.OutgoingMessage is null)
      throw new SagaConfigurationError(
          "A compensation transition must provide a compensating IIntegrationEvent.");

    if (transition.Type == SagaTransitionType.Fail && transition.Error is null)
      throw new SagaConfigurationError("A failed saga transition must provide an exception.");

    if (transition.Type == SagaTransitionType.Fail && transition.OutgoingMessage is not null)
      throw new SagaConfigurationError("A failed saga transition cannot publish an outgoing message.");

    var now = DateTimeOffset.UtcNow;
    var nextStatus = transition.Type switch
    {
      SagaTransitionType.Continue => record.Status == SagaLifecycleStatus.Compensating
          ? SagaLifecycleStatus.Compensating
          : SagaLifecycleStatus.Running,
      SagaTransitionType.Complete => SagaLifecycleStatus.Completed,
      SagaTransitionType.Compensate => SagaLifecycleStatus.Compensating,
      SagaTransitionType.Fail => SagaLifecycleStatus.Failed,
      SagaTransitionType.Retry => SagaLifecycleStatus.RetryScheduled,
      _ => throw new SagaConfigurationError(
          $"Saga transition type '{transition.Type}' is not a supported lifecycle outcome.")
    };

    var updatedRecord = record with
    {
      Status = nextStatus,
      UpdatedAt = now,
      CompletedAt = nextStatus == SagaLifecycleStatus.Completed ? now : record.CompletedAt,
      FailedAt = nextStatus == SagaLifecycleStatus.Failed ? now : record.FailedAt,
      NextAttemptAt = null,
      RetryCount = retryLimitExceeded ? record.RetryCount : 0,
      LastMessageId = messageId,
      LastError = transition.Error?.Message ?? record.LastError
    };

    await _repository.SaveAsync(updatedRecord, expectedVersion, cancellationToken).ConfigureAwait(false);

    await WriteAuditAsync(
        updatedRecord,
        transition,
        incomingMessageType,
        stepType,
        correlationId,
        causationId,
        messageId,
        tenantId,
        duration,
        cancellationToken).ConfigureAwait(false);

    if (transition.OutgoingMessage is IIntegrationEvent outgoingEvent)
    {
      var outgoingContext = MediatorContext.Current
          .WithCorrelationId(correlationId)
          .WithTenant(tenantId);
      if (messageId.HasValue)
        outgoingContext = outgoingContext.WithCausationId(messageId);

      using var contextScope = MediatorContext.BeginScope(outgoingContext);

      if (_messageStore is not null)
      {
        // Use the Messaging package's canonical event envelope and durable outbox contract.
        // Provider-level transaction enlistment is required for atomic state/outbox commits.
        var outgoingMessage = new IntegrationEventMessageBuilderStrategy().Build(outgoingEvent);
        outgoingMessage.MessageType = outgoingEvent.GetType().AssemblyQualifiedName
            ?? outgoingEvent.GetType().FullName
            ?? outgoingEvent.GetType().Name;

        await _messageStore.SaveAsync(outgoingMessage, cancellationToken).ConfigureAwait(false);
      }
      else
      {
        // Preserve compatibility for hosts that have not configured the Messaging outbox.
        await _publisher.Publish(outgoingEvent, cancellationToken).ConfigureAwait(false);
      }
    }

    if (transition.Type == SagaTransitionType.Fail)
      throw new SagaFailedException(record.SagaId, transition.Error);
  }

  private async Task WriteAuditAsync(
      SagaPersistenceRecord updatedRecord,
      ISagaTransition transition,
      Type incomingMessageType,
      string stepType,
      Guid correlationId,
      Guid? causationId,
      Guid? messageId,
      string? tenantId,
      TimeSpan duration,
      CancellationToken cancellationToken)
  {
    if (!_options.EnableAuditing || _auditSinks.Count == 0)
      return;

    var auditRecord = new SagaAuditRecord
    {
      SagaId = updatedRecord.SagaId,
      SagaType = updatedRecord.SagaType,
      StateType = updatedRecord.State.GetType().AssemblyQualifiedName ?? updatedRecord.State.GetType().FullName!,
      StepType = stepType,
      IncomingMessageType = incomingMessageType.AssemblyQualifiedName ?? incomingMessageType.FullName!,
      OutgoingMessageType = transition.OutgoingMessage?.GetType().AssemblyQualifiedName,
      CorrelationId = correlationId,
      CausationId = causationId,
      MessageId = messageId,
      TenantId = tenantId,
      Status = updatedRecord.Status,
      Timestamp = updatedRecord.UpdatedAt,
      Duration = duration,
      ErrorType = transition.Error?.GetType().FullName
    };

    foreach (var sink in _auditSinks)
    {
      try
      {
        await sink.WriteAsync(auditRecord, cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception exception)
      {
        _logger.LogError(
            exception,
            "Saga audit sink {AuditSink} failed after saga {SagaId} committed transition {Status}.",
            sink.GetType().FullName,
            updatedRecord.SagaId,
            updatedRecord.Status);
      }
    }
  }

  private async Task<ISagaTransition> ExecuteHandlerAsync(
      object saga,
      IIntegrationEvent message,
      MethodInfo handler,
      ISagaContext context,
      CancellationToken cancellationToken)
  {
    object? result = null;

    await _pipeline.ExecuteAsync(async () =>
    {
      try
      {
        result = handler.Invoke(saga, new object[] { message, context, cancellationToken });
      }
      catch (TargetInvocationException exception) when (exception.InnerException is not null)
      {
        ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        throw;
      }

      if (result is not Task task)
        throw new SagaConfigurationError(
            $"Saga '{saga.GetType().Name}' handler '{handler.Name}' returned a null task.");

      await task.ConfigureAwait(false);
    }).ConfigureAwait(false);

    if (result is Task<ISagaTransition> transitionTask)
      return await transitionTask.ConfigureAwait(false)
          ?? throw new SagaConfigurationError(
              $"Saga '{saga.GetType().Name}' handler '{handler.Name}' returned a null transition.");

    return SagaTransition.Continue(null);
  }

  private static async Task CallOnCreatedAsync(
      object saga,
      ISagaContext context,
      CancellationToken cancellationToken)
  {
    var method = saga.GetType().GetMethod(
        "OnCreatedAsync",
        BindingFlags.Instance | BindingFlags.Public,
        binder: null,
        types: new[] { typeof(ISagaContext), typeof(CancellationToken) },
        modifiers: null);

    if (method is null)
      throw new SagaConfigurationError(
          $"Saga '{saga.GetType().Name}' must expose OnCreatedAsync(ISagaContext, CancellationToken).");

    var result = method.Invoke(saga, new object[] { context, cancellationToken });

    if (result is not Task task)
      throw new SagaConfigurationError(
          $"Saga '{saga.GetType().Name}' OnCreatedAsync must return Task.");

    await task.ConfigureAwait(false);
  }

  private static Guid ResolveCorrelationId(SagaPersistenceRecord record, Guid incomingCorrelationId)
  {
    if (record.CorrelationId != Guid.Empty &&
        incomingCorrelationId != Guid.Empty &&
        record.CorrelationId != incomingCorrelationId)
      throw new SagaCorrelationException(
          record.SagaId,
          record.CorrelationId,
          incomingCorrelationId);

    if (record.CorrelationId != Guid.Empty)
      return record.CorrelationId;

    return incomingCorrelationId == Guid.Empty
        ? Guid.CreateVersion7()
        : incomingCorrelationId;
  }

  private static Guid GetSagaId(object saga)
  {
    var property = saga.GetType().GetProperty("SagaId");

    return property?.GetValue(saga) is Guid sagaId && sagaId != Guid.Empty
        ? sagaId
        : throw new SagaConfigurationError(
            $"Saga '{saga.GetType().Name}' must have a non-empty Guid SagaId before its start handler executes.");
  }

  private static void SetSagaId(object saga, Guid sagaId)
  {
    var method = saga.GetType().GetMethod("SetId", new[] { typeof(Guid) })
        ?? throw new SagaConfigurationError(
            $"Saga '{saga.GetType().Name}' must expose public SetId(Guid) for identity rehydration.");

    method.Invoke(saga, new object[] { sagaId });

    if (GetSagaId(saga) != sagaId)
      throw new SagaConfigurationError(
          $"Saga '{saga.GetType().Name}' did not retain the persisted saga identity.");
  }

  private static void EnsureSagaIdentityUnchanged(object saga, Guid expectedSagaId)
  {
    if (GetSagaId(saga) != expectedSagaId)
      throw new SagaConfigurationError(
          $"Saga '{saga.GetType().Name}' changed its identity during execution.");
  }

  private static ISagaState GetSagaState(object saga, SagaRegistration registration)
  {
    var stateProperty = registration.SagaType.GetProperty("State")
        ?? throw new SagaConfigurationError(
            $"Saga '{registration.SagaType.Name}' does not expose a State property.");

    return stateProperty.GetValue(saga) as ISagaState
        ?? throw new SagaConfigurationError(
            $"Saga '{registration.SagaType.Name}' returned a null or invalid State.");
  }

  private static Guid ResolveSagaId(
      object saga,
      IIntegrationEvent message,
      SagaRegistration registration)
  {
    var messageType = message.GetType();
    var resolverType = typeof(ISagaIdResolver<>).MakeGenericType(messageType);

    if (!resolverType.IsInstanceOfType(saga))
      throw new SagaConfigurationError(
          $"Saga '{registration.SagaType.Name}' must implement " +
          $"ISagaIdResolver<{messageType.Name}> to resolve the saga identity for message '{messageType.Name}'.");

    var resolver = resolverType.GetMethod(nameof(ISagaIdResolver<IIntegrationEvent>.ResolveSagaId))
        ?? throw new SagaConfigurationError(
            $"Saga ID resolver method was not found for message '{messageType.Name}'.");

    var result = resolver.Invoke(saga, new object[] { message });

    return result is Guid sagaId && sagaId != Guid.Empty
        ? sagaId
        : throw new SagaConfigurationError(
            $"Saga '{registration.SagaType.Name}' resolved an empty or invalid SagaId for message '{messageType.Name}'.");
  }

  private static void SetSagaState(object saga, SagaRegistration registration, ISagaState state)
  {
    var stateProperty = registration.SagaType.GetProperty("State")
        ?? throw new SagaConfigurationError(
            $"Saga '{registration.SagaType.Name}' does not expose a State property.");

    if (!stateProperty.PropertyType.IsInstanceOfType(state))
      throw new SagaConfigurationError(
          $"State '{state.GetType().Name}' is incompatible with saga '{registration.SagaType.Name}'.");

    stateProperty.SetValue(saga, state);
  }
}
