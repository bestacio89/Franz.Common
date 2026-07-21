#nullable enable

using Franz.Common.Business.Domain;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Core;

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

  public async Task HandleEventAsync(
      IIntegrationEvent incomingEvent,
      Guid correlationId,
      Guid? causationId,
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
      Guid correlationId,
      Guid? causationId,
      CancellationToken token)
  {
    var msgType = evt.GetType();
    bool isStart = reg.CanStartWith(msgType);

    object saga;
    ISagaState state;

    if (isStart)
    {
      // ----------------------------
      // CREATE NEW SAGA
      // ----------------------------

      saga = _services.GetService(reg.SagaType)
          ?? throw new InvalidOperationException(
              $"Saga not found: {reg.SagaType.Name}");

      state = (ISagaState)Activator.CreateInstance(reg.StateType)!;

      reg.SagaType.GetProperty("State")!
          .SetValue(saga, state);

      var sagaId = GetSagaId(saga);

      var ctx = new SagaContext(
          sagaId,
          reg.SagaType,
          state,
          evt,
          correlationId,
          causationId,
          token);

      await CallOnCreatedAsync(saga, ctx, token);

      await ExecuteHandlerAsync(
          saga,
          evt,
          reg.StartHandlers[msgType],
          ctx,
          token);

      await _repository.SaveStateAsync(sagaId, state, token);
      return;
    }

    // ----------------------------
    // LOAD EXISTING SAGA
    // ----------------------------

    Guid sagaIdExisting = ExtractCorrelationId(evt, reg);

    state = (ISagaState)(await _repository.LoadStateAsync(
        sagaIdExisting,
        reg.StateType,
        token)
        ?? throw new SagaNotFoundException(
            $"Saga {reg.SagaType.Name} with ID {sagaIdExisting} not found."));

    saga = _services.GetService(reg.SagaType)
        ?? throw new InvalidOperationException(
            $"Saga not found: {reg.SagaType.Name}");

    reg.SagaType.GetProperty("State")!
        .SetValue(saga, state);

    var context = new SagaContext(
        sagaIdExisting,
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
      return;

    await ExecuteHandlerAsync(
        saga,
        evt,
        handler,
        context,
        token);

    await _repository.SaveStateAsync(
        sagaIdExisting,
        state,
        token);
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
      var correlationGuid = MediatorContext.CorrelationId;
      MediatorContext.EnsureCorrelationId(); // Ensure a correlation ID is present in the MediatorContext
      await _publisher.Publish(integrationEvent);
    }
  }

  private async Task CallOnCreatedAsync(
      object saga,
      ISagaContext context,
      CancellationToken token)
  {
    var createdMethod = saga.GetType().GetMethod("OnCreatedAsync");

    if (createdMethod != null)
    {
      var task = (Task?)createdMethod.Invoke(
          saga,
          new object[] { context, token });

      if (task != null)
        await task;
    }
  }

  private static Guid GetSagaId(object saga)
  {
    var prop = saga.GetType().GetProperty("SagaId")
        ?? throw new SagaConfigurationError("SagaId property missing.");

    return prop.GetValue(saga) is Guid id && id != Guid.Empty
        ? id
        : throw new SagaConfigurationError("SagaId must be a non-empty Guid.");
  }

  private static Guid ExtractCorrelationId(
      IIntegrationEvent message,
      SagaRegistration reg)
  {
    var msgType = message.GetType();
    var corrType = typeof(IMessageCorrelation<>).MakeGenericType(msgType);
    var impl = reg.SagaType.GetInterface(corrType.FullName!);

    if (impl == null)
      throw new SagaConfigurationError(
          $"Saga {reg.SagaType.Name} lacks correlation for {msgType.Name}");

    var method = impl.GetMethod("GetCorrelationId")
        ?? throw new SagaConfigurationError("GetCorrelationId not found.");

    var result = method.Invoke(null, new object[] { message });

    return result switch
    {
      Guid guid => guid,
      _ => throw new SagaConfigurationError(
          "CorrelationId must be Guid.")
    };
  }
}