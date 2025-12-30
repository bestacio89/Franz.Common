#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Tests.Events;

namespace Franz.Common.Messaging.Sagas.Tests.Sagas;

#nullable enable

public sealed class TestSaga :
  SagaBase<TestSagaState>,
  IStartWith<StartEvent>,
  IHandle<StepEvent>,
  ICompensateWith<CompensationEvent>,
  IMessageCorrelation<StartEvent>,
  IMessageCorrelation<StepEvent>
{
  public override Task OnCreatedAsync(
    ISagaContext context,
    CancellationToken ct)
  {
    State.Id = context.CorrelationId
      ?? context.Message switch
      {
        StartEvent e => e.Id,
        StepEvent e => e.Id,
        _ => throw new InvalidOperationException(
          "Unable to derive saga id from message")
      };

    State.Counter = 0;
    return Task.CompletedTask;
  }

  public Task<ISagaTransition> HandleAsync(
    StartEvent message,
    ISagaContext context,
    CancellationToken ct)
  {
    State.Counter = 1;
    return Task.FromResult(SagaTransition.Continue(null));
  }

  public Task<ISagaTransition> HandleAsync(
    StepEvent message,
    ISagaContext context,
    CancellationToken ct)
  {
    State.Counter++;
    return Task.FromResult(SagaTransition.Continue(null));
  }

  public Task<ISagaTransition> HandleAsync(
    CompensationEvent message,
    ISagaContext context,
    CancellationToken ct)
  {
    State.Counter--;
    return Task.FromResult(SagaTransition.Continue(null));
  }

  public string GetCorrelationId(StartEvent message) => message.Id;
  public string GetCorrelationId(StepEvent message) => message.Id;
}
