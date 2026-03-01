#nullable enable
using Franz.Common.Business.Domain;
using Franz.Common.Mediator;
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
        ISagaState state;
        object saga;

        var msgType = evt.GetType();
        bool isStart = reg.CanStartWith(msgType);

        if (isStart)
        {
            saga = _services.GetService(reg.SagaType)
                ?? throw new InvalidOperationException($"Saga not found: {reg.SagaType.Name}");

            state = (ISagaState)Activator.CreateInstance(reg.StateType)!;

            // Fix CS0246: Now using the interface defined above
            if (state is ISagaStateWithGuid guidState && guidState.Id == Guid.Empty)
                guidState.Id = Guid.CreateVersion7();

            reg.SagaType.GetProperty("State")!.SetValue(saga, state);

            var ctx = new SagaContext(
                null!, 
                reg.SagaType,
                state,
                evt,
                correlationId,
                causationId,
                token);

            // Fix CS0103: Added the private helper method below
            await CallOnCreatedAsync(saga, ctx, token);

            var finalId = GetSagaId(saga);

            ctx = new SagaContext(
                finalId,
                reg.SagaType,
                state,
                evt,
                correlationId,
                causationId,
                token);

            await ExecuteHandlerAsync(saga, evt, reg.StartHandlers[msgType], ctx, token);
            await _repository.SaveStateAsync(finalId, state, token);
            return;
        }

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

        if (handler is null) return;

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
            if (result is Task t) await t;
        });

        ISagaTransition? transition = result is Task<ISagaTransition> ttrans
                ? await ttrans
                : SagaTransition.Continue(null);

        if (transition?.OutgoingMessage is IIntegrationEvent integrationEvent)
        {
            // Fix CS1501: If your Publisher doesn't support the options lambda yet,
            // we rely on the CorrelationId.Current ambient context we set in the Handler.
            CorrelationId.Current = (Guid)context.CorrelationId;
            await _publisher.Publish(integrationEvent);
        }
    }

    // Fix CS0103: The missing helper method
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
            throw new SagaConfigurationException($"Saga {reg.SagaType.Name} lacks correlation for {msgType.Name}");

        var method = impl.GetMethod("GetCorrelationId")
            ?? throw new SagaConfigurationException("GetCorrelationId not found.");

        return (string)method.Invoke(null, new object[] { message })!;
    }
}