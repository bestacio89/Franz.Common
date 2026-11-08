using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

namespace Franz.Common.Messaging.Sagas.Tests.Execution;

public sealed class SagaStartTests
{
  [Fact]
  public async Task HandleEventAsync_GeneratesSagaIdBeforeCreationAndStartHandler()
  {
    var saga = new StartingSaga();
    var repository = new InMemorySagaRepository(
        new InMemorySagaStateStore(),
        new JsonSagaStateSerializer());
    var services = new TestServiceProvider(saga);
    var router = new SagaRouter(services);
    router.RegisterSaga<StartingSaga>();
    var orchestrator = new SagaOrchestrator(
      router,
      repository,
      new SagaExecutionPipeline(),
      new TestMessagingPublisher(),
      services);

    await orchestrator.HandleEventAsync(
      new StartEvent(),
      Guid.CreateVersion7(),
      null);

    var persisted = await repository.LoadAsync(
        saga.SagaId,
        tenantId: null,
        typeof(StartingSagaState),
        CancellationToken.None);

    saga.SagaId.Should().NotBe(Guid.Empty);
    saga.IdObservedDuringCreation.Should().Be(saga.SagaId);
    saga.IdObservedByHandler.Should().Be(saga.SagaId);
    persisted.Should().NotBeNull();
    persisted!.Version.Should().Be(1);
    persisted.Status.Should().Be(SagaLifecycleStatus.Running);
  }

  private sealed class StartingSaga :
    SagaBase<StartingSagaState>,
    IStartWith<StartEvent>
  {
    public Guid? IdObservedDuringCreation { get; private set; }
    public Guid? IdObservedByHandler { get; private set; }

    public override Task OnCreatedAsync(
      ISagaContext context,
      CancellationToken cancellationToken = default)
    {
      IdObservedDuringCreation = context.SagaId;
      return Task.CompletedTask;
    }

    public Task<ISagaTransition> HandleAsync(
      StartEvent message,
      ISagaContext context,
      CancellationToken cancellationToken = default)
    {
      IdObservedByHandler = context.SagaId;
      return Task.FromResult<ISagaTransition>(context.Continue());
    }
  }

  public sealed class StartingSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record StartEvent : IIntegrationEvent;

  private sealed class TestServiceProvider(StartingSaga saga) : IServiceProvider
  {
    public object? GetService(Type serviceType)
      => serviceType == typeof(StartingSaga) ? saga : null;
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
