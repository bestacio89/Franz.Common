using FluentAssertions;
using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Handlers;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Storage;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Serialization;

namespace Franz.Common.Messaging.Sagas.Tests.Handlers;

public sealed class SagaDispatchingMessageHandlerTenantTests
{
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task ProcessAsync_RejectsBlankTenantHeaderInsteadOfUsingDefaultScope(string tenantId)
  {
    var fixture = new Fixture();
    var message = fixture.CreateMessage();
    message.Headers[HeaderConstants.TenantId] = [tenantId];

    var act = () => fixture.Handler.ProcessAsync(message);

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*tenant*");
    (await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(TestSagaState))).Should().BeNull();
  }

  [Fact]
  public async Task ProcessAsync_RejectsAmbiguousTenantHeaderValues()
  {
    var fixture = new Fixture();
    var message = fixture.CreateMessage();
    message.Headers[HeaderConstants.TenantId] = ["tenant-a", "tenant-b"];

    var act = () => fixture.Handler.ProcessAsync(message);

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*tenant*");
  }

  [Fact]
  public async Task ProcessAsync_PreservesValidatedTenantContextAsRepositoryScope()
  {
    var fixture = new Fixture();
    var tenantId = Guid.CreateVersion7();
    var message = fixture.CreateMessage();
    message.Headers[HeaderConstants.TenantId] = [tenantId.ToString("D")];
    var tenantContext = new TenantExecutionContext(
        new TenantInfo(tenantId, "tenant-a"),
        domainId: null,
        source: TenantResolutionSource.Header,
        resolvedAt: DateTimeOffset.UtcNow);

    await fixture.CreateHandler(new FixedTenantExecutionContextAccessor(tenantContext))
        .ProcessAsync(message);

    (await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, tenantId.ToString("D"), typeof(TestSagaState))).Should().NotBeNull();
    (await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(TestSagaState))).Should().BeNull();
  }

  [Fact]
  public async Task ProcessAsync_RejectsTenantHeaderThatDoesNotMatchValidatedContext()
  {
    var fixture = new Fixture();
    var headerTenantId = Guid.CreateVersion7();
    var validatedTenantId = Guid.CreateVersion7();
    var message = fixture.CreateMessage();
    message.Headers[HeaderConstants.TenantId] = [headerTenantId.ToString("D")];
    var tenantContext = new TenantExecutionContext(
        new TenantInfo(validatedTenantId, "validated-tenant"),
        domainId: null,
        source: TenantResolutionSource.Header,
        resolvedAt: DateTimeOffset.UtcNow);

    var act = () => fixture.CreateHandler(new FixedTenantExecutionContextAccessor(tenantContext))
        .ProcessAsync(message);

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*does not match the validated tenant execution context*");
  }

  [Fact]
  public async Task ProcessAsync_RejectsEmptyMessageIdentityWhenInboxIsEnabled()
  {
    var fixture = new Fixture();
    var operations = new List<string>();
    var message = fixture.CreateMessage();
    message.Id = Guid.Empty;
    var handler = new SagaDispatchingMessageHandler(
        fixture.Orchestrator,
        new JsonMessageSerializer(),
        new RecordingInboxStore(operations),
        new RecordingUnitOfWork(operations));

    var act = () => handler.ProcessAsync(message);

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*non-empty message identity*");
    operations.Should().BeEmpty();
  }

  [Fact]
  public async Task ProcessAsync_CommitsFailedLifecycleStateBeforeSurfacingFailure()
  {
    var fixture = new Fixture();
    fixture.Saga.FailOnStart = true;
    var operations = new List<string>();
    var handler = new SagaDispatchingMessageHandler(
        fixture.Orchestrator,
        new JsonMessageSerializer(),
        new RecordingInboxStore(operations),
        new RecordingUnitOfWork(operations));

    var act = () => handler.ProcessAsync(fixture.CreateMessage());

    await act.Should().ThrowAsync<SagaFailedException>();
    operations.Should().Equal("begin", "inbox", "commit");

    var persisted = await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(TestSagaState));
    persisted.Should().NotBeNull();
    persisted!.Status.ToString().Should().Be("Failed");
  }

  [Fact]
  public async Task ProcessAsync_RollsBackWhenSagaExecutionThrowsBeforeCommit()
  {
    var fixture = new Fixture();
    fixture.Saga.ThrowOnStart = true;
    var operations = new List<string>();
    var handler = new SagaDispatchingMessageHandler(
        fixture.Orchestrator,
        new JsonMessageSerializer(),
        new RecordingInboxStore(operations),
        new RecordingUnitOfWork(operations));

    var act = () => handler.ProcessAsync(fixture.CreateMessage());

    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Expected unexpected saga execution failure.");
    operations.Should().Equal("begin", "inbox", "rollback");
  }

  [Fact]
  public async Task ProcessAsync_RollsBackAndSkipsExecutionWhenInboxReceiptAlreadyExists()
  {
    var fixture = new Fixture();
    var operations = new List<string>();
    var handler = new SagaDispatchingMessageHandler(
        fixture.Orchestrator,
        new JsonMessageSerializer(),
        new DuplicateInboxStore(operations),
        new RecordingUnitOfWork(operations));

    await handler.ProcessAsync(fixture.CreateMessage());

    operations.Should().Equal("begin", "inbox", "rollback");
    (await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(TestSagaState))).Should().BeNull();
  }

  [Fact]
  public async Task ProcessAsync_ResolvesIntegrationEventEnvelopeProducedByCanonicalMessageBuilder()
  {
    var fixture = new Fixture();
    var message = new IntegrationEventMessageBuilderStrategy().Build(new StartEvent());

    await fixture.Handler.ProcessAsync(message);

    (await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(TestSagaState))).Should().NotBeNull();
  }

  [Fact]
  public async Task ProcessAsync_RejectsTenantHeaderWithoutValidatedExecutionContext()
  {
    var fixture = new Fixture();
    var message = fixture.CreateMessage();
    message.Headers[HeaderConstants.TenantId] = [Guid.CreateVersion7().ToString("D")];

    var act = () => fixture.Handler.ProcessAsync(message);

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*validated tenant execution context*");
  }

  [Fact]
  public async Task ProcessAsync_UsesDefaultScopeOnlyWhenTenantHeaderIsAbsent()
  {
    var fixture = new Fixture();

    await fixture.Handler.ProcessAsync(fixture.CreateMessage());

    (await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(TestSagaState))).Should().NotBeNull();
  }

  private sealed class Fixture
  {
    public TestSaga Saga { get; } = new();
    public InMemorySagaRepository Repository { get; }
    public SagaDispatchingMessageHandler Handler => CreateHandler();
    public SagaOrchestrator Orchestrator { get; }

    public Fixture()
    {
      Repository = new InMemorySagaRepository(
          new InMemorySagaStateStore(),
          new JsonSagaStateSerializer());

      var services = new TestServiceProvider(Saga);
      var router = new SagaRouter(services);
      router.RegisterSaga(typeof(TestSaga));

      Orchestrator = new SagaOrchestrator(
          router,
          Repository,
          new SagaExecutionPipeline(),
          new TestMessagingPublisher(),
          services);

    }

    public SagaDispatchingMessageHandler CreateHandler(
        ITenantExecutionContextAccessor? tenantExecutionContextAccessor = null)
        => new(
            Orchestrator,
            new JsonMessageSerializer(),
            inboxStore: null,
            persistenceUnitOfWork: null,
            tenantExecutionContextAccessor: tenantExecutionContextAccessor);

    public Message CreateMessage()
        => new("{}")
        {
          MessageType = typeof(StartEvent).AssemblyQualifiedName
        };
  }

  private sealed class TestSaga : SagaBase<TestSagaState>, IStartWith<StartEvent>
  {
    public bool FailOnStart { get; set; }
    public bool ThrowOnStart { get; set; }

    public Task<ISagaTransition> HandleAsync(
        StartEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
    {
      if (ThrowOnStart)
        throw new InvalidOperationException("Expected unexpected saga execution failure.");

      return Task.FromResult(FailOnStart
          ? context.Fail(new InvalidOperationException("Expected saga failure."))
          : context.Continue());
    }
  }

  private sealed class FixedTenantExecutionContextAccessor(TenantExecutionContext context)
      : ITenantExecutionContextAccessor
  {
    public TenantExecutionContext? Current { get; private set; } = context;

    public void Initialize(TenantExecutionContext value) => Current = value;

    public void Clear() => Current = null;
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

  private sealed class DuplicateInboxStore(List<string> operations) : IInboxStore
  {
    public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default)
    {
      operations.Add("inbox");
      return Task.FromResult(false);
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

  private sealed class TestSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record StartEvent : IIntegrationEvent;

  private sealed class TestServiceProvider(object saga) : IServiceProvider
  {
    public object? GetService(Type serviceType)
        => serviceType == saga.GetType() ? saga : null;
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
