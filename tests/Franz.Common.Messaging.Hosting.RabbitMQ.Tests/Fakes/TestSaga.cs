using Franz.Common.Messaging.Sagas.Abstractions;

public sealed class TestSaga
  : ISaga<TestSagaState>
{
  public static readonly SagaStepId Step1 = new("step-1");
  public static readonly SagaStepId Step2 = new("step-2");

  public ValueTask ExecuteAsync(
    SagaContext<TestSagaState> context,
    CancellationToken ct)
  {
    context.State.ExecutedSteps.Add(context.StepId);
    return ValueTask.CompletedTask;
  }

  public ValueTask CompensateAsync(
    SagaContext<TestSagaState> context,
    CancellationToken ct)
  {
    context.State.CompensatedSteps.Add(context.StepId);
    return ValueTask.CompletedTask;
  }
}
