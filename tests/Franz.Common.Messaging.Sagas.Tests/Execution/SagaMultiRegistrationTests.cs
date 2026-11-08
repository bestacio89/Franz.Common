using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Handlers;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Messaging.Storage;

namespace Franz.Common.Messaging.Sagas.Tests.Execution;

public sealed class SagaMultiRegistrationTests
{
  [Fact]
  public async Task DispatchAsync_ExecutesEveryMatchingSagaAndCommitsOneSharedUnitOfWork()
  {
    var failingSaga = new FailingSaga();
    var succeedingSaga = new SucceedingSaga();
    var repository = new InMemorySagaRepository(
        new InMemorySagaStateStore(),
        new JsonSagaStateSerializer());
    var services = new SagaServiceProvider(failingSaga, succeedingSaga);
    var router = new SagaRouter(services);
    router.RegisterSaga<FailingSaga>();
    router.RegisterSaga<SucceedingSaga>();

    var orchestrator = new SagaOrchestrator(
        router,
        repository,
        new SagaExecutionPipeline(),
        new TestMessagingPublisher(),
        services);
    var operations = new List<string>();
    var handler = new SagaDispatchingMessageHandler(
        orchestrator,
        new JsonMessageSerializer(),
        new RecordingInboxStore(operations),
        new RecordingUnitOfWork(operations));

    var message = new Message("{}")
    {
      MessageType = typeof(SharedEvent).AssemblyQualifiedName
    };

    var act = () => handler.ProcessAsync(message);

    await act.Should().ThrowAsync<SagaFailedException>();
    operations.Should().Equal("begin", "inbox", "commit");

    var failed = await repository.LoadAsync(
        failingSaga.SagaId, null, typeof(FailingSagaState));
    var succeeded = await repository.LoadAsync(
        succeedingSaga.SagaId, null, typeof(SucceedingSagaState));

    failed.Should().NotBeNull();
    failed!.Status.Should().Be(SagaLifecycleStatus.Failed);
    succeeded.Should().NotBeNull();
    succeeded!.Status.Should().Be(SagaLifecycleStatus.Completed);
    succeedingSaga.HandlerInvoked.Should().BeTrue();
  }

  private sealed class FailingSaga : SagaBase<FailingSagaState>, IStartWith<SharedEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        SharedEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Fail(new InvalidOperationException("expected terminal failure")));
  }

  private sealed class SucceedingSaga : SagaBase<SucceedingSagaState>, IStartWith<SharedEvent>
  {
    public bool HandlerInvoked { get; private set; }

    public Task<ISagaTransition> HandleAsync(
        SharedEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
    {
      HandlerInvoked = true;
      return Task.FromResult(context.Complete());
    }
  }

  private sealed class FailingSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed class SucceedingSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record SharedEvent : IIntegrationEvent;

  private sealed class SagaServiceProvider(params object[] sagas) : IServiceProvider
  {
    public object? GetService(Type serviceType)
        => sagas.SingleOrDefault(saga => saga.GetType() == serviceType);
  }

  private sealed class RecordingInboxStore(List<string> operations) : IInboxStore
  {
    public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default)
    {
      operations.Add("inbox");
      return Task.FromResult(true);
    }

    public Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
        => Task.CompletedTask;
  }

  private sealed class RecordingUnitOfWork(List<string> operations) : IUnitOfWork
  {
    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
      operations.Add("begin");
      return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
      operations.Add("commit");
      return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
      operations.Add("rollback");
      return Task.CompletedTask;
    }
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
