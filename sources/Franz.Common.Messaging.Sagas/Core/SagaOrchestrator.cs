#nullable enable

using Franz.Common.Business.Domain;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using System;
using System.Reflection;
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
    ArgumentNullException.ThrowIfNull(incomingEvent);

    var eventType = incomingEvent.GetType();

    foreach (var registration in _router.ResolveRegistrationsForMessage(eventType))
    {
      await ExecuteSagaForMessageAsync(
          incomingEvent,
          registration,
          correlationId,
          causationId,
          cancellationToken).ConfigureAwait(false);
    }
  }

  private async Task ExecuteSagaForMessageAsync(
      IIntegrationEvent message,
      SagaRegistration registration,
      Guid correlationId,
      Guid? causationId,
      CancellationToken cancellationToken)
  {
    var messageType = message.GetType();

    var saga = _services.GetService(registration.SagaType)
        ?? throw new SagaConfigurationError(
            $"Saga '{registration.SagaType.FullName}' is not registered in the service provider.");

    if (registration.CanStartWith(messageType))
    {
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
          correlationId,
          causationId,
          cancellationToken);

      await CallOnCreatedAsync(saga, context, cancellationToken).ConfigureAwait(false);

      await ExecuteHandlerAsync(
          saga,
          message,
          registration.StartHandlers[messageType],
          context,
          cancellationToken).ConfigureAwait(false);

      await _repository.SaveStateAsync(sagaId, state, cancellationToken).ConfigureAwait(false);
      return;
    }

    var sagaIdExisting = ResolveSagaId(saga, message, registration);
    var existingState = await _repository.LoadStateAsync(
        sagaIdExisting,
        registration.StateType,
        cancellationToken).ConfigureAwait(false) as ISagaState
        ?? throw new SagaNotFoundException(
            $"Saga '{registration.SagaType.Name}' with ID '{sagaIdExisting}' was not found.");

    SetSagaState(saga, registration, existingState);

    var executionContext = new SagaContext(
        sagaIdExisting,
        registration.SagaType,
        existingState,
        message,
        correlationId,
        causationId,
        cancellationToken);

    var handler = registration.StepHandlers.TryGetValue(messageType, out var stepHandler)
        ? stepHandler
        : registration.CompensationHandlers.GetValueOrDefault(messageType);

    if (handler is null)
      throw new SagaConfigurationError(
          $"Saga '{registration.SagaType.Name}' was routed message '{messageType.Name}' " +
          "but no step or compensation handler was registered.");

    await ExecuteHandlerAsync(
        saga,
        message,
        handler,
        executionContext,
        cancellationToken).ConfigureAwait(false);

    await _repository.SaveStateAsync(
        sagaIdExisting,
        existingState,
        cancellationToken).ConfigureAwait(false);
  }

  private async Task ExecuteHandlerAsync(
      object saga,
      IIntegrationEvent message,
      MethodInfo handler,
      ISagaContext context,
      CancellationToken cancellationToken)
  {
    object? result = null;

    await _pipeline.ExecuteAsync(async () =>
    {
      result = handler.Invoke(saga, new object[] { message, context, cancellationToken });

      if (result is Task task)
        await task.ConfigureAwait(false);
    }).ConfigureAwait(false);

    var transition = result is Task<ISagaTransition> transitionTask
        ? await transitionTask.ConfigureAwait(false)
        : SagaTransition.Continue(null);

    if (transition.OutgoingMessage is IIntegrationEvent outgoingEvent)
    {
      MediatorContext.EnsureCorrelationId();
      await _publisher.Publish(outgoingEvent, cancellationToken).ConfigureAwait(false);
    }
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

  private static Guid GetSagaId(object saga)
  {
    var property = saga.GetType().GetProperty("SagaId");

    return property?.GetValue(saga) is Guid sagaId && sagaId != Guid.Empty
        ? sagaId
        : throw new SagaConfigurationError(
            $"Saga '{saga.GetType().Name}' must have a non-empty Guid SagaId before its start handler executes.");
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

  private static void SetSagaState(
      object saga,
      SagaRegistration registration,
      ISagaState state)
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
