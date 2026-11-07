using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

namespace Franz.Common.Messaging.Sagas.Tests.Execution;

public sealed class SagaLifecycleTests
{
  [Fact]
  public async Task HandlerReturningNullTask_FailsWithoutPersistingSaga()
  {
    var saga = new NullTaskSaga();
    var repository = CreateRepository();
    var orchestrator = CreateOrchestrator(saga, repository);

    var act = () => orchestrator.HandleEventAsync(
        new StartEvent(),
        Guid.CreateVersion7(),
        null);

    await act.Should().ThrowAsync<Exception>()
        .WithMessage("*returned a null task*");

    var record = await repository.LoadAsync(
        saga.SagaId,
        tenantId: null,
        typeof(TestSagaState),
        CancellationToken.None);

    record.Should().BeNull();
  }

  [Fact]
  public async Task CompleteTransition_PersistsTerminalStatusAndTimestamp()
  {
    var saga = new CompletingSaga();
    var repository = CreateRepository();
    var orchestrator = CreateOrchestrator(saga, repository);

    await orchestrator.HandleEventAsync(new StartEvent(), Guid.CreateVersion7(), null);

    var record = await repository.LoadAsync(
        saga.SagaId,
        tenantId: null,
        typeof(TestSagaState),
        CancellationToken.None);

    record.Should().NotBeNull();
    record!.Status.Should().Be(SagaLifecycleStatus.Completed);
    record.CompletedAt.Should().NotBeNull();
    record.FailedAt.Should().BeNull();
  }

  [Fact]
  public async Task FailTransition_PersistsFailureBeforeSurfacingFailure()
  {
    var saga = new FailingSaga();
    var repository = CreateRepository();
    var orchestrator = CreateOrchestrator(saga, repository);

    var act = () => orchestrator.HandleEventAsync(
        new StartEvent(),
        Guid.CreateVersion7(),
        null);

    await act.Should().ThrowAsync<SagaFailedException>();

    var record = await repository.LoadAsync(
        saga.SagaId,
        tenantId: null,
        typeof(TestSagaState),
        CancellationToken.None);

    record.Should().NotBeNull();
    record!.Status.Should().Be(SagaLifecycleStatus.Failed);
    record.FailedAt.Should().NotBeNull();
    record.LastError.Should().Be("payment declined");
  }

  [Fact]
  public async Task RetryTransition_FailsFastWhenNoDurableSchedulerIsConfigured()
  {
    var saga = new RetryingSaga();
    var repository = CreateRepository();
    var orchestrator = CreateOrchestrator(saga, repository);

    var act = () => orchestrator.HandleEventAsync(
        new StartEvent(),
        Guid.CreateVersion7(),
        null);

    var exception = await act.Should().ThrowAsync<Exception>();
    exception.WithMessage("*durable retry scheduler*");

    var record = await repository.LoadAsync(
        saga.SagaId,
        tenantId: null,
        typeof(TestSagaState),
        CancellationToken.None);

    record.Should().BeNull();
  }

  [Fact]
  public async Task CompensationTransition_EntersCompensatingAndCompletesAfterCompensationHandler()
  {
    var saga = new CompensatingSaga();
    var repository = CreateRepository();
    var publisher = new TestMessagingPublisher();
    var orchestrator = CreateOrchestrator(saga, repository, publisher);

    var correlationId = Guid.CreateVersion7();
    await orchestrator.HandleEventAsync(new StartEvent(), correlationId, null);

    var compensating = await repository.LoadAsync(
        saga.SagaId,
        tenantId: null,
        typeof(TestSagaState),
        CancellationToken.None);

    compensating.Should().NotBeNull();
    compensating!.Status.Should().Be(SagaLifecycleStatus.Compensating);
    publisher.Published.Should().ContainSingle().Which.Should().BeOfType<CompensationRequestedEvent>();

    await orchestrator.HandleEventAsync(
        new CompensationRequestedEvent(saga.SagaId),
        correlationId,
        null);

    var completed = await repository.LoadAsync(
        saga.SagaId,
        tenantId: null,
        typeof(TestSagaState),
        CancellationToken.None);

    completed.Should().NotBeNull();
    completed!.Status.Should().Be(SagaLifecycleStatus.Completed);
    completed.CompletedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task CompensationHandler_CannotRunBeforeSagaEntersCompensatingState()
  {
    var saga = new PrematureCompensationSaga();
    var repository = CreateRepository();
    var orchestrator = CreateOrchestrator(saga, repository);
    var correlationId = Guid.CreateVersion7();

    await orchestrator.HandleEventAsync(new StartEvent(), correlationId, null);

    var act = () => orchestrator.HandleEventAsync(
        new CompensationRequestedEvent(saga.SagaId),
        correlationId,
        null);

    var exception = await act.Should().ThrowAsync<SagaLifecycleException>();
    exception.Which.Status.Should().Be(SagaLifecycleStatus.Running);
    saga.CompensationHandlerInvoked.Should().BeFalse();

    var record = await repository.LoadAsync(
        saga.SagaId,
        tenantId: null,
        typeof(TestSagaState),
        CancellationToken.None);

    record.Should().NotBeNull();
    record!.Status.Should().Be(SagaLifecycleStatus.Running);
    record.Version.Should().Be(1);
  }

  private static InMemorySagaRepository CreateRepository()
      => new(new InMemorySagaStateStore(), new JsonSagaStateSerializer());

  private static SagaOrchestrator CreateOrchestrator(
      object saga,
      InMemorySagaRepository repository,
      TestMessagingPublisher? publisher = null)
  {
    var services = new TestServiceProvider(saga);
    var router = new SagaRouter(services);
    router.RegisterSaga(saga.GetType());

    return new SagaOrchestrator(
        router,
        repository,
        new SagaExecutionPipeline(),
        publisher ?? new TestMessagingPublisher(),
        services);
  }

  private sealed class NullTaskSaga : SagaBase<TestSagaState>, IStartWith<StartEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        StartEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => null!;
  }

  private sealed class CompletingSaga : SagaBase<TestSagaState>, IStartWith<StartEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        StartEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Complete());
  }

  private sealed class FailingSaga : SagaBase<TestSagaState>, IStartWith<StartEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        StartEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Fail(new InvalidOperationException("payment declined")));
  }

  private sealed class RetryingSaga : SagaBase<TestSagaState>, IStartWith<StartEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        StartEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Retry(TimeSpan.FromSeconds(10)));
  }

  private sealed class CompensatingSaga :
      SagaBase<TestSagaState>,
      IStartWith<StartEvent>,
      ICompensateWith<CompensationRequestedEvent>,
      ISagaIdResolver<CompensationRequestedEvent>
  {
    public Guid ResolveSagaId(CompensationRequestedEvent message) => message.SagaId;

    public Task<ISagaTransition> HandleAsync(
        StartEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Compensate(new CompensationRequestedEvent(context.SagaId)));

    public Task<ISagaTransition> HandleAsync(
        CompensationRequestedEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Complete());
  }

  private sealed class PrematureCompensationSaga :
      SagaBase<TestSagaState>,
      IStartWith<StartEvent>,
      ICompensateWith<CompensationRequestedEvent>,
      ISagaIdResolver<CompensationRequestedEvent>
  {
    public bool CompensationHandlerInvoked { get; private set; }

    public Guid ResolveSagaId(CompensationRequestedEvent message) => message.SagaId;

    public Task<ISagaTransition> HandleAsync(
        StartEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Continue());

    public Task<ISagaTransition> HandleAsync(
        CompensationRequestedEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
    {
      CompensationHandlerInvoked = true;
      return Task.FromResult(context.Complete());
    }
  }

  public sealed class TestSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record StartEvent : IIntegrationEvent;
  private sealed record CompensationRequestedEvent(Guid SagaId) : IIntegrationEvent;

  private sealed class TestServiceProvider(object saga) : IServiceProvider
  {
    public object? GetService(Type serviceType)
        => serviceType == saga.GetType() ? saga : null;
  }

  private sealed class TestMessagingPublisher : IMessagingPublisher
  {
    public List<IIntegrationEvent> Published { get; } = new();

    public ValueTask Publish<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken ct = default)
        where TIntegrationEvent : IIntegrationEvent
    {
      Published.Add(integrationEvent);
      return ValueTask.CompletedTask;
    }
  }
}
