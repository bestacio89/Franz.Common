#nullable enable

using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Logging;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Franz.Common.Messaging.Sagas.Tests.Core;

public sealed class SagaRetryTests
{
  [Fact]
  public async Task Retry_SchedulesTheOriginalEventAndResumesTheSameSagaAfterTheDelay()
  {
    var fixture = new Fixture(maxRetryAttempts: 3);
    var originalMessageId = Guid.CreateVersion7();
    var correlationId = Guid.CreateVersion7();
    var causationId = Guid.CreateVersion7();
    var originalMessage = new IntegrationEventMessageBuilderStrategy().Build(new RetryEvent());
    originalMessage.Id = originalMessageId;
    originalMessage.CorrelationId = correlationId;

    await fixture.Orchestrator.HandleEventAsync(
        new RetryEvent(),
        correlationId,
        causationId,
        originalMessageId,
        tenantId: null,
        retrySagaId: null,
        originalMessage);

    var scheduled = fixture.MessageStore.ScheduledMessages.Should().ContainSingle().Which;
    var persisted = await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(RetrySagaState));

    persisted.Should().NotBeNull();
    persisted!.Status.Should().Be(SagaLifecycleStatus.RetryScheduled);
    persisted.RetryCount.Should().Be(1);
    persisted.LastMessageId.Should().Be(scheduled.Message.Id);
    persisted.NextAttemptAt.Should().Be(scheduled.NotBefore);
    scheduled.Message.Id.Should().NotBe(originalMessageId);
    scheduled.Message.CorrelationId.Should().Be(correlationId);
    scheduled.Message.Headers[Message.CausationHeader].Should().ContainSingle()
        .Which.Should().Be(originalMessageId.ToString("D"));
    scheduled.Message.Headers[SagaRetryHeaders.TargetSagaId].Should().ContainSingle()
        .Which.Should().Be(fixture.Saga.SagaId.ToString("D"));
    scheduled.NotBefore.Should().BeAfter(DateTimeOffset.UtcNow);

    var retrySagaId = Guid.Parse(
        scheduled.Message.Headers[SagaRetryHeaders.TargetSagaId].Single());

    var earlyDelivery = () => fixture.Orchestrator.HandleEventAsync(
        new RetryEvent(),
        correlationId,
        originalMessageId,
        scheduled.Message.Id,
        tenantId: null,
        retrySagaId,
        scheduled.Message);

    await earlyDelivery.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*not due*");

    await Task.Delay(scheduled.NotBefore - DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(20));

    await fixture.Orchestrator.HandleEventAsync(
        new RetryEvent(),
        correlationId,
        originalMessageId,
        scheduled.Message.Id,
        tenantId: null,
        retrySagaId,
        scheduled.Message);

    persisted = await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(RetrySagaState));

    persisted.Should().NotBeNull();
    persisted!.Status.Should().Be(SagaLifecycleStatus.Completed);
    persisted.RetryCount.Should().Be(0);
    persisted.NextAttemptAt.Should().BeNull();
    ((RetrySagaState)persisted.State).Attempts.Should().Be(2);
    fixture.MessageStore.ScheduledMessages.Should().ContainSingle();
  }

  [Fact]
  public async Task Retry_FailsSagaWhenTheConfiguredRetryLimitIsExceeded()
  {
    var fixture = new Fixture(maxRetryAttempts: 1, alwaysRetry: true);
    var correlationId = Guid.CreateVersion7();
    var originalMessageId = Guid.CreateVersion7();
    var originalMessage = new IntegrationEventMessageBuilderStrategy().Build(new RetryEvent());
    originalMessage.Id = originalMessageId;
    originalMessage.CorrelationId = correlationId;

    await fixture.Orchestrator.HandleEventAsync(
        new RetryEvent(),
        correlationId,
        causationId: null,
        originalMessageId,
        tenantId: null,
        retrySagaId: null,
        originalMessage);

    var scheduled = fixture.MessageStore.ScheduledMessages.Should().ContainSingle().Which;
    await Task.Delay(scheduled.NotBefore - DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(20));

    var retrySagaId = Guid.Parse(
        scheduled.Message.Headers[SagaRetryHeaders.TargetSagaId].Single());

    var act = () => fixture.Orchestrator.HandleEventAsync(
        new RetryEvent(),
        correlationId,
        originalMessageId,
        scheduled.Message.Id,
        tenantId: null,
        retrySagaId,
        scheduled.Message);

    await act.Should().ThrowAsync<SagaFailedException>();

    var persisted = await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(RetrySagaState));

    persisted.Should().NotBeNull();
    persisted!.Status.Should().Be(SagaLifecycleStatus.Failed);
    persisted.RetryCount.Should().Be(1);
    persisted.NextAttemptAt.Should().BeNull();
    fixture.MessageStore.ScheduledMessages.Should().ContainSingle();
  }

  [Fact]
  public async Task Retry_RequiresTransactionalPersistenceAndAScheduledMessageStore()
  {
    var saga = new RetrySaga();
    var services = new TestServiceProvider(saga);
    var repository = new InMemorySagaRepository(
        new InMemorySagaStateStore(),
        new JsonSagaStateSerializer());
    var router = new SagaRouter(services);
    router.RegisterSaga(typeof(RetrySaga));
    var orchestrator = new SagaOrchestrator(
        router,
        repository,
        new SagaExecutionPipeline(),
        new TestMessagingPublisher(),
        services);

    var act = () => orchestrator.HandleEventAsync(
        new RetryEvent(),
        Guid.CreateVersion7(),
        causationId: null,
        Guid.CreateVersion7(),
        tenantId: null);

    await act.Should().ThrowAsync<SagaConfigurationError>()
        .WithMessage("*durable retry*");
    (await repository.LoadAsync(saga.SagaId, null, typeof(RetrySagaState))).Should().BeNull();
  }

  public sealed record RetryEvent : IIntegrationEvent;

  public sealed class RetrySaga : SagaBase<RetrySagaState>, IStartWith<RetryEvent>
  {
    public bool AlwaysRetry { get; set; }

    public Task<ISagaTransition> HandleAsync(
        RetryEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
    {
      var state = (RetrySagaState)context.State;
      state.Attempts++;
      if (AlwaysRetry || state.Attempts == 1)
        return Task.FromResult(context.Retry(
            TimeSpan.FromMilliseconds(75),
            new InvalidOperationException("Transient dependency failure.")));

      return Task.FromResult(context.Complete());
    }
  }

  public sealed class RetrySagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
    public int Attempts { get; set; }
  }

  private sealed class Fixture
  {
    public RetrySaga Saga { get; } = new();
    public InMemorySagaRepository Repository { get; }
    public RecordingMessageStore MessageStore { get; } = new();
    public SagaOrchestrator Orchestrator { get; }

    public Fixture(int maxRetryAttempts, bool alwaysRetry = false)
    {
      Saga.AlwaysRetry = alwaysRetry;
      var services = new TestServiceProvider(Saga);
      Repository = new InMemorySagaRepository(
          new InMemorySagaStateStore(),
          new JsonSagaStateSerializer());
      var router = new SagaRouter(services);
      router.RegisterSaga(typeof(RetrySaga));

      Orchestrator = new SagaOrchestrator(
          router,
          Repository,
          new SagaExecutionPipeline(),
          new TestMessagingPublisher(),
          services,
          Array.Empty<ISagaAuditSink>(),
          new FranzSagaOptions
          {
            Persistence = "EntityFramework",
            MaxRetryAttempts = maxRetryAttempts,
            DefaultRetryDelay = TimeSpan.FromMilliseconds(75)
          },
          NullLogger<SagaOrchestrator>.Instance,
          MessageStore,
          new RecordingUnitOfWork());
    }
  }

  private sealed class TestServiceProvider(object saga) : IServiceProvider
  {
    public object? GetService(Type serviceType)
        => serviceType == saga.GetType() ? saga : null;
  }

  private sealed class RecordingUnitOfWork : IUnitOfWork
  {
    public Task BeginAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
  }

  private sealed class RecordingMessageStore : IMessageStore
  {
    public List<(Message Message, DateTimeOffset NotBefore)> ScheduledMessages { get; } = [];

    public Task SaveAsync(Message message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SaveScheduledAsync(
        Message message,
        DateTimeOffset notBefore,
        CancellationToken cancellationToken = default)
    {
      ScheduledMessages.Add((message, notBefore));
      return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<StoredMessage>>([]);

    public Task MarkAsSentAsync(Guid messageId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
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
