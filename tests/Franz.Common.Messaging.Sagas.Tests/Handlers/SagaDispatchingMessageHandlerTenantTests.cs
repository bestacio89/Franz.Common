using FluentAssertions;
using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Handlers;
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
  public async Task ProcessAsync_PreservesValidTenantHeaderAsRepositoryScope()
  {
    var fixture = new Fixture();
    var message = fixture.CreateMessage();
    message.Headers[HeaderConstants.TenantId] = ["tenant-a"];

    await fixture.Handler.ProcessAsync(message);

    (await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, "tenant-a", typeof(TestSagaState))).Should().NotBeNull();
    (await fixture.Repository.LoadAsync(
        fixture.Saga.SagaId, null, typeof(TestSagaState))).Should().BeNull();
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
    public SagaDispatchingMessageHandler Handler { get; }

    public Fixture()
    {
      Repository = new InMemorySagaRepository(
          new InMemorySagaStateStore(),
          new JsonSagaStateSerializer());

      var services = new TestServiceProvider(Saga);
      var router = new SagaRouter(services);
      router.RegisterSaga(typeof(TestSaga));

      var orchestrator = new SagaOrchestrator(
          router,
          Repository,
          new SagaExecutionPipeline(),
          new TestMessagingPublisher(),
          services);

      Handler = new SagaDispatchingMessageHandler(orchestrator, new JsonMessageSerializer());
    }

    public Message CreateMessage()
        => new("{}")
        {
          MessageType = typeof(StartEvent).AssemblyQualifiedName
        };
  }

  private sealed class TestSaga : SagaBase<TestSagaState>, IStartWith<StartEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        StartEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Continue());
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
