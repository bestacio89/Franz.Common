using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Logging;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Franz.Common.Messaging.Sagas.Tests.Execution;

public sealed class SagaAuditTests
{
  [Fact]
  public async Task Orchestrator_WritesMetadataOnlyAuditRecordWhenAuditingIsEnabled()
  {
    var saga = new AuditedSaga();
    var repository = new InMemorySagaRepository(new InMemorySagaStateStore(), new JsonSagaStateSerializer());
    var sink = new TestAuditSink();
    var services = new TestServiceProvider(saga);
    var router = new SagaRouter(services);
    router.RegisterSaga<AuditedSaga>();

    var orchestrator = new SagaOrchestrator(
        router, repository, new SagaExecutionPipeline(), new TestMessagingPublisher(), services,
        new[] { sink }, new FranzSagaOptions { EnableAuditing = true },
        NullLogger<SagaOrchestrator>.Instance);

    var correlationId = Guid.CreateVersion7();
    await orchestrator.HandleEventAsync(new StartEvent(), correlationId, null);

    sink.Records.Should().ContainSingle();
    var record = sink.Records.Single();
    record.SagaId.Should().Be(saga.SagaId);
    record.StepType.Should().Be("Start");
    record.IncomingMessageType.Should().Contain(nameof(StartEvent));
    record.CorrelationId.Should().Be(correlationId);
    record.Status.Should().Be(SagaLifecycleStatus.Completed);
    record.StateType.Should().Contain(nameof(TestSagaState));
  }

  [Fact]
  public async Task Orchestrator_PropagatesDurableAuditFailures()
  {
    var saga = new AuditedSaga();
    var repository = new InMemorySagaRepository(new InMemorySagaStateStore(), new JsonSagaStateSerializer());
    var store = new FailingAuditStore();
    var services = new TestServiceProvider(saga);
    var router = new SagaRouter(services);
    router.RegisterSaga<AuditedSaga>();

    var orchestrator = new SagaOrchestrator(
        router, repository, new SagaExecutionPipeline(), new TestMessagingPublisher(), services,
        new ISagaAuditSink[] { store },
        new FranzSagaOptions { EnableAuditing = true },
        NullLogger<SagaOrchestrator>.Instance);

    var act = () => orchestrator.HandleEventAsync(new StartEvent(), Guid.CreateVersion7(), null);

    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("durable audit write failed");
    store.WriteAttempts.Should().Be(1);
  }

  [Fact]
  public async Task Orchestrator_ContinuesWhenBestEffortAuditSinkFails()
  {
    var saga = new AuditedSaga();
    var repository = new InMemorySagaRepository(new InMemorySagaStateStore(), new JsonSagaStateSerializer());
    var sink = new FailingBestEffortAuditSink();
    var services = new TestServiceProvider(saga);
    var router = new SagaRouter(services);
    router.RegisterSaga<AuditedSaga>();

    var orchestrator = new SagaOrchestrator(
        router, repository, new SagaExecutionPipeline(), new TestMessagingPublisher(), services,
        new ISagaAuditSink[] { sink }, new FranzSagaOptions { EnableAuditing = true },
        NullLogger<SagaOrchestrator>.Instance);

    await orchestrator.HandleEventAsync(new StartEvent(), Guid.CreateVersion7(), null);

    sink.WriteAttempts.Should().Be(1);
  }

  [Fact]
  public void Registration_RejectsAuditRetentionForMemoryPersistence()
  {
    var services = new ServiceCollection();

    var act = () => services.AddFranzSagas(options =>
    {
      options.Persistence = "Memory";
      options.AuditRetentionPeriod = TimeSpan.FromDays(30);
    });

    act.Should().Throw<NotSupportedException>()
        .WithMessage("*durable EF Core, MongoDB, or Cosmos persistence*");
  }

  [Fact]
  public void Registration_RejectsNonPositiveAuditRetentionPeriod()
  {
    var services = new ServiceCollection();

    var act = () => services.AddFranzSagas(options =>
    {
      options.Persistence = "EntityFramework";
      options.AuditRetentionPeriod = TimeSpan.Zero;
    });

    act.Should().Throw<ArgumentOutOfRangeException>()
        .WithParameterName("AuditRetentionPeriod");
  }

  private sealed class FailingBestEffortAuditSink : ISagaAuditSink
  {
    public int WriteAttempts { get; private set; }

    public Task WriteAsync(SagaAuditRecord record, CancellationToken cancellationToken = default)
    {
      WriteAttempts++;
      throw new InvalidOperationException("best-effort audit sink failed");
    }
  }

  private sealed class FailingAuditStore : ISagaAuditStore
  {
    public int WriteAttempts { get; private set; }

    public Task WriteAsync(SagaAuditRecord record, CancellationToken cancellationToken = default)
    {
      WriteAttempts++;
      throw new InvalidOperationException("durable audit write failed");
    }

    public Task<int> DeleteExpiredAsync(
        DateTimeOffset cutoff,
        int batchSize,
        CancellationToken cancellationToken = default)
        => Task.FromResult(0);
  }

  private sealed class AuditedSaga : SagaBase<TestSagaState>, IStartWith<StartEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        StartEvent message, ISagaContext context, CancellationToken cancellationToken)
        => Task.FromResult(context.Complete());
  }

  public sealed class TestSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record StartEvent : IIntegrationEvent;

  private sealed class TestServiceProvider(AuditedSaga saga) : IServiceProvider
  {
    public object? GetService(Type serviceType)
        => serviceType == typeof(AuditedSaga) ? saga : null;
  }

  private sealed class TestMessagingPublisher : IMessagingPublisher
  {
    public ValueTask Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct = default)
        where TIntegrationEvent : IIntegrationEvent
        => ValueTask.CompletedTask;
  }

  private sealed class TestAuditSink : ISagaAuditSink
  {
    public List<SagaAuditRecord> Records { get; } = new();

    public Task WriteAsync(SagaAuditRecord record, CancellationToken cancellationToken = default)
    {
      Records.Add(record);
      return Task.CompletedTask;
    }
  }
}
