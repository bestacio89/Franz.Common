#nullable enable

using Franz.Common.Business.Domain;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Executes saga steps and dispatches outgoing integration events.
/// </summary>
public sealed class SagaOrchestrator
{
  private readonly SagaRouter _router;
  private readonly ISagaRepository _repository;
  private readonly SagaExecutionPipeline _pipeline;
  private readonly IMessagingPublisher _publisher;
  private readonly IServiceProvider _services;

  public SagaOrchestrator(
      SagaRouter router,
      ISagaRepository repository,
      SagaExecutionPipeline pipeline,
      IMessagingPublisher publisher,
      IServiceProvider services)
  {
    _router = router;
    _repository = repository;
    _pipeline = pipeline;
    _publisher = publisher;
    _services = services;
  }

  /// <summary>
  /// Handles an incoming integration event and routes it to connected sagas.
  /// </summary>
  public async Task HandleEventAsync(
      IIntegrationEvent incomingEvent,
      string? correlationId,
      string? causationId,
      CancellationToken cancellationToken = default)
  {
    var eventType = incomingEvent.GetType();

    foreach (var reg in _router.ResolveRegistrationsForMessage(eventType))
    {
      await ExecuteSagaForMessageAsync(
          incomingEvent,
          reg,
          correlationId,
          causationId,
          cancellationToken);
    }
  }

  private async Task ExecuteSagaForMessageAsync(
      IIntegrationEvent evt,
      SagaRegistration reg,
      string? correlationId,
      string? causationId,
      CancellationToken token)
  {
    ISagaState state;
    object saga;

    var msgType = evt.GetType();
    bool isStart = reg.CanStartWith(msgType);

    if (isStart)
    {
      // -----------------------------
      // NEW SAGA INSTANCE
      // -----------------------------
      saga = _services.GetService(reg.SagaType)
          ?? throw new InvalidOperationException($"Saga not found: {reg.SagaType.Name}");

      state = (ISagaState)Activator.CreateInstance(reg.StateType)!;

      // attach state to saga
      reg.SagaType.GetProperty("State")!.SetValue(saga, state);

      // At this point the saga/state have not yet derived their ID.
      // We create a temporary context with an empty SagaId.
      var ctx = new SagaContext(
          sagaId: string.Empty,
          reg.SagaType,
          state,
          evt,
          correlationId,
          causationId,
          token);

      // Let the saga initialize itself (usually sets SagaId/State.Id)
      await CallOnCreatedAsync(saga, ctx, token);

      // Now retrieve the REAL saga id
      var sagaId = GetSagaId(saga);
      if (string.IsNullOrWhiteSpace(sagaId))
        throw new SagaConfigurationException(
          $"Saga {reg.SagaType.Name} returned empty SagaId after OnCreatedAsync.");

      // Execute the start handler
      var startHandler = reg.StartHandlers[msgType];
      await ExecuteHandlerAsync(saga, evt, startHandler, ctx, token);

      // Persist state using the resolved saga id
      await _repository.SaveStateAsync(sagaId, state, token);
      return;
    }

    // -----------------------------
    // EXISTING SAGA INSTANCE
    // -----------------------------
    var existingSagaId = ExtractCorrelationId(evt, reg);

    state = (ISagaState)(await _repository.LoadStateAsync(existingSagaId, reg.StateType, token)
        ?? throw new SagaNotFoundException($"Saga {reg.SagaType.Name} with ID {existingSagaId} not found."));

    saga = _services.GetService(reg.SagaType)
        ?? throw new InvalidOperationException($"Saga not found: {reg.SagaType.Name}");

    reg.SagaType.GetProperty("State")!.SetValue(saga, state);

    var context = new SagaContext(
        existingSagaId,
        reg.SagaType,
        state,
        evt,
        correlationId,
        causationId,
        token);

    var handler = reg.StepHandlers.ContainsKey(msgType)
        ? reg.StepHandlers[msgType]
        : reg.CompensationHandlers.GetValueOrDefault(msgType);

    if (handler is null)
      return; // nothing to do

    await ExecuteHandlerAsync(saga, evt, handler, context, token);

    await _repository.SaveStateAsync(existingSagaId, state, token);
  }

  private async Task ExecuteHandlerAsync(
      object saga,
      IIntegrationEvent message,
      System.Reflection.MethodInfo handler,
      ISagaContext context,
      CancellationToken token)
  {
    object? result = null;

    await _pipeline.ExecuteAsync(async () =>
    {
      result = handler.Invoke(saga, new object[] { message, context, token });
      if (result is Task t)
        await t;
    });

    ISagaTransition? transition =
        result is Task<ISagaTransition> ttrans
            ? await ttrans
            : SagaTransition.Continue(null);

    if (transition?.OutgoingMessage is IIntegrationEvent integrationEvent)
    {
      // Dispatch using the real Franz messaging pipeline
      await _publisher.Publish(integrationEvent);
    }
  }

  private async Task CallOnCreatedAsync(object saga, ISagaContext context, CancellationToken token)
  {
    var createdMethod = saga.GetType().GetMethod("OnCreatedAsync");
    if (createdMethod != null)
    {
      var t = (Task?)createdMethod.Invoke(saga, new object[] { context, token });
      if (t != null) await t;
    }
  }

  private static string GetSagaId(object saga)
  {
    var prop = saga.GetType().GetProperty("SagaId")
        ?? throw new SagaConfigurationException("SagaId property missing.");
    return prop.GetValue(saga)?.ToString()
        ?? throw new SagaConfigurationException("SagaId returned null.");
  }

  private static string ExtractCorrelationId(IIntegrationEvent message, SagaRegistration reg)
  {
    var msgType = message.GetType();
    var corrType = typeof(IMessageCorrelation<>).MakeGenericType(msgType);

    var impl = reg.SagaType.GetInterface(corrType.FullName!);

    if (impl == null)
      throw new SagaConfigurationException(
          $"Saga {reg.SagaType.Name} does not implement IMessageCorrelation<{msgType.Name}>");

    var method = impl.GetMethod("GetCorrelationId")
        ?? throw new SagaConfigurationException("GetCorrelationId not found.");

    return (string)method.Invoke(null, new object[] { message })!;
  }
}
