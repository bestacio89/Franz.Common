#nullable enable

using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Storage;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

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
          Array.Empty<ISagaAuditSink>(), new FranzSagaOptions(), NullLogger<SagaOrchestrator>.Instance)
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
      ILogger<SagaOrchestrator>? logger = null,
      IMessageStore? messageStore = null)
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
          cancellationToken);

  public async Task HandleEventAsync(
      IIntegrationEvent incomingEvent,
      Guid correlationId,
      Guid? causationId,
      Guid? messageId,
      string? tenantId,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(incomingEvent);
    cancellationToken.ThrowIfCancellationRequested();

    var messageType = incomingEvent.GetType();

    foreach (var registration in _router.ResolveRegistrationsForMessage(messageType))
    {
      await ExecuteSagaForMessageAsync(
          incomingEvent,
          registration,
          correlationId,
          causationId,
          messageId,
          tenantId,
          cancellationToken).ConfigureAwait(false);
    }
  }

  private async Task ExecuteSagaForMessageAsync(
      IIntegrationEvent message,
      SagaRegistration registration,
      Guid correlationId,
      Guid? causationId,
      Guid? messageId,
      string? tenantId,
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
      var sagaId = GetSagaId(saga);
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

    if (transition.Type == SagaTransitionType.Retry)
      throw new SagaConfigurationError(
          "Saga retry is not enabled: a durable retry scheduler and atomic scheduling boundary are not configured.");

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
          $"ISagaIdResolver<{messageType.Name}> to process non-start message '{messageType.Name}'.");

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
