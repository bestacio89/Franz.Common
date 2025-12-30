#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Tests.Events;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Tests.Sagas;

public sealed class TestSaga :
  SagaBase<TestSagaState>,
  IStartWith<StartEvent>,
  IHandle<StepEvent>,
  ICompensateWith<CompensationEvent>,
  IMessageCorrelation<StepEvent>
{
  public Task<ISagaTransition> HandleAsync(
    StartEvent message,
    ISagaContext context,
    CancellationToken ct)
  {
    State.Id = message.Id;
    State.Counter = 1;

    // Explicitly continue without emitting a message
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

  // ✅ INSTANCE method — correctly implements IMessageCorrelation<T>
  public string GetCorrelationId(StepEvent message)
    => message.Id;
}
