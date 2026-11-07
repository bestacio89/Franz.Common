using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

namespace Franz.Common.Messaging.Sagas.Tests.Execution;

public sealed class SagaCorrelationTests
{
  [Fact]
  public async Task HandleEventAsync_ResolvesSagaIdOnSagaInstance_AndKeepsCorrelationSeparate()
  {
    var expectedSagaId = Guid.CreateVersion7();
    var correlationId = Guid.CreateVersion7();
    var now = DateTimeOffset.UtcNow;
    var saga = new CorrelatedSaga(expectedSagaId);
    var repository = new InMemorySagaRepository(
        new InMemorySagaStateStore(),
        new JsonSagaStateSerializer());

    await repository.SaveAsync(
        new SagaPersistenceRecord
        {
          SagaId = expectedSagaId,
          SagaType = typeof(CorrelatedSaga).AssemblyQualifiedName!,
          State = new CorrelatedSagaState(),
          Status = SagaLifecycleStatus.Running,
          CreatedAt = now,
          UpdatedAt = now
        },
        expectedVersion: null,
        CancellationToken.None);

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

    var persisted = await repository.LoadAsync(
        expectedSagaId,
        tenantId: null,
        typeof(CorrelatedSagaState),
        CancellationToken.None);

    saga.SagaId.Should().Be(expectedSagaId);
    saga.SagaIdResolverCalled.Should().BeTrue();
    saga.HandlerCalled.Should().BeTrue();
    saga.ObservedSagaId.Should().Be(expectedSagaId);
    saga.ObservedCorrelationId.Should().Be(correlationId);
    persisted.Should().NotBeNull();
    persisted!.Version.Should().Be(2);
    persisted.Status.Should().Be(SagaLifecycleStatus.Running);
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

  public sealed class CorrelatedSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record StepEvent : IIntegrationEvent;

  private sealed class TestServiceProvider(CorrelatedSaga saga) : IServiceProvider
  {
    public object? GetService(Type serviceType)
      => serviceType == typeof(CorrelatedSaga) ? saga : null;
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
