using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence;

namespace Franz.Common.Messaging.Sagas.Tests.Execution;

public sealed class SagaCorrelationTests
{
  [Fact]
  public async Task HandleEventAsync_ResolvesCorrelationOnSagaInstance()
  {
    var saga = new CorrelatedSaga();
    var services = new TestServiceProvider(saga);
    var router = new SagaRouter(services);
    router.RegisterSaga<CorrelatedSaga>();

    var orchestrator = new SagaOrchestrator(
      router,
      new TestSagaRepository(),
      new SagaExecutionPipeline(),
      new TestMessagingPublisher(),
      services);

    await Record.ExceptionAsync(
      () => orchestrator.HandleEventAsync(new StepEvent(), Guid.NewGuid(), null));

    saga.CorrelationResolverCalled.Should().BeTrue();
  }

  private sealed class CorrelatedSaga :
    SagaBase<CorrelatedSagaState>,
    IHandle<StepEvent>,
    IMessageCorrelation<StepEvent>
  {
    public bool CorrelationResolverCalled { get; private set; }

    public string GetCorrelationId(StepEvent message)
    {
      CorrelationResolverCalled = true;
      return "correlation-key";
    }

    public Task<ISagaTransition> HandleAsync(
      StepEvent message,
      ISagaContext context,
      CancellationToken cancellationToken = default)
      => Task.FromResult<ISagaTransition>(context.Continue());
  }

  private sealed class CorrelatedSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record StepEvent : IIntegrationEvent;

  private sealed class TestServiceProvider(CorrelatedSaga saga) : IServiceProvider
  {
    public object? GetService(Type serviceType)
      => serviceType == typeof(CorrelatedSaga) ? saga : null;
  }

  private sealed class TestSagaRepository : ISagaRepository
  {
    public Task<object?> LoadStateAsync(
      Guid sagaId,
      Type stateType,
      CancellationToken cancellationToken)
      => Task.FromResult<object?>(null);

    public Task SaveStateAsync(
      Guid sagaId,
      object state,
      CancellationToken cancellationToken)
      => Task.CompletedTask;

    public Task DeleteStateAsync(
      Guid sagaId,
      CancellationToken cancellationToken)
      => Task.CompletedTask;
  }

  private sealed class TestMessagingPublisher : IMessagingPublisher
  {
    public ValueTask Publish<TIntegrationEvent>(
      TIntegrationEvent integrationEvent,
      CancellationToken ct = default)
      where TIntegrationEvent : IIntegrationEvent
      => ValueTask.CompletedTask;
  }
}
