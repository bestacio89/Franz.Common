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
  public async Task HandleEventAsync_ResolvesSagaIdOnSagaInstance_AndKeepsCorrelationSeparate()
  {
    var expectedSagaId = Guid.CreateVersion7();
    var correlationId = Guid.CreateVersion7();
    var state = new CorrelatedSagaState();
    var saga = new CorrelatedSaga(expectedSagaId);
    var repository = new TestSagaRepository(state);
    var services = new TestServiceProvider(saga);
    var router = new SagaRouter(services);
    router.RegisterSaga<CorrelatedSaga>();

    var orchestrator = new SagaOrchestrator(
      router,
      repository,
      new SagaExecutionPipeline(),
      new TestMessagingPublisher(),
      services);

    await orchestrator.HandleEventAsync(
      new StepEvent(),
      correlationId,
      Guid.CreateVersion7());

    saga.SagaIdResolverCalled.Should().BeTrue();
    saga.HandlerCalled.Should().BeTrue();
    repository.LoadedSagaId.Should().Be(expectedSagaId);
    saga.ObservedSagaId.Should().Be(expectedSagaId);
    saga.ObservedCorrelationId.Should().Be(correlationId);
  }

  private sealed class CorrelatedSaga(Guid expectedSagaId) :
    SagaBase<CorrelatedSagaState>,
    IHandle<StepEvent>,
    ISagaIdResolver<StepEvent>
  {
    public bool SagaIdResolverCalled { get; private set; }
    public bool HandlerCalled { get; private set; }
    public Guid? ObservedSagaId { get; private set; }
    public Guid? ObservedCorrelationId { get; private set; }

    public Guid ResolveSagaId(StepEvent message)
    {
      SagaIdResolverCalled = true;
      return expectedSagaId;
    }

    public Task<ISagaTransition> HandleAsync(
      StepEvent message,
      ISagaContext context,
      CancellationToken cancellationToken = default)
    {
      HandlerCalled = true;
      ObservedSagaId = context.SagaId;
      ObservedCorrelationId = context.CorrelationId;
      return Task.FromResult<ISagaTransition>(context.Continue());
    }
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

  private sealed class TestSagaRepository(CorrelatedSagaState state) : ISagaRepository
  {
    public Guid? LoadedSagaId { get; private set; }

    public Task<object?> LoadStateAsync(
      Guid sagaId,
      Type stateType,
      CancellationToken cancellationToken)
    {
      LoadedSagaId = sagaId;
      return Task.FromResult<object?>(state);
    }

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
