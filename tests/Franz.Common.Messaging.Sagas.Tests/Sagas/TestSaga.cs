#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Tests.Events;

namespace Franz.Common.Messaging.Sagas.Tests.Sagas;

public sealed class TestSaga :
  SagaBase<TestSagaState>,
  IStartWith<StartEvent>,
  IHandle<StepEvent>,
  ICompensateWith<CompensationEvent>,
  IMessageCorrelation<StartEvent>,
  IMessageCorrelation<StepEvent>
{
  // 🔑 CRITICAL: saga identity MUST be established before any handler runs
  public override Task OnCreatedAsync(
    ISagaContext context,
    CancellationToken ct)
  {
    State.Id = context.CorrelationId
      ?? throw new InvalidOperationException("CorrelationId is required");

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

  // 🔑 Correlation must exist for EVERY entry point
  public string GetCorrelationId(StartEvent message)
    => message.Id;

  public string GetCorrelationId(StepEvent message)
    => message.Id;
}
