#nullable enable

using FluentAssertions;
using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Registration;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Hosting.Delegating;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Messaging.Sagas.Tests.Handlers;

public sealed class SagaIntegrationEventNotificationHandlerTests
{
  static SagaIntegrationEventNotificationHandlerTests()
  {
    // Mirrors the generated module registration for this test-owned event contract.
    GeneratedMediatorInvokerRegistry.RegisterNotification<StartedEvent>();
  }

  [Fact]
  public async Task NotificationPipeline_RejectsTenantHeaderWithoutValidatedExecutionContext()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IMessagingPublisher>(new TestMessagingPublisher());

    var message = new Message("{}");
    message.Headers[HeaderConstants.TenantId] = [Guid.CreateVersion7().ToString("D")];
    message.Headers[MessagingConstants.ClassName] =
        [HeaderNamer.GetEventClassName(typeof(StartedEvent))];

    services.AddSingleton<IMessageContextAccessor>(
        new TestMessageContextAccessor(new MessageContext(message)));

    services
        .AddFranzSagas(options => options.Persistence = "Memory")
        .AddSaga<StartedEventSaga>();

    using var provider = services.BuildServiceProvider();
    provider.BuildFranzSagas();

    await using var scope = provider.CreateAsyncScope();
    var dispatcher = new FranzDispatcher(scope.ServiceProvider);
    var executer = new MessagingStrategyExecuter(
        Array.Empty<IAsyncMessageActionFilter>(),
        dispatcher);

    var act = () => executer.ExecuteAsync(message);

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*validated tenant execution context*");
    provider.GetRequiredService<InMemorySagaStateStore>().Store.Should().BeEmpty();
  }

  [Fact]
  public async Task NotificationPipeline_RejectsMissingOriginalMessageContextWhenInboxIsEnabled()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IMessagingPublisher>(new TestMessagingPublisher());
    var inboxStore = new RecordingInboxStore();
    services.AddSingleton<IInboxStore>(inboxStore);

    var message = new Message("{}");
    message.Headers[MessagingConstants.ClassName] =
        [HeaderNamer.GetEventClassName(typeof(StartedEvent))];

    services
        .AddFranzSagas(options => options.Persistence = "Memory")
        .AddSaga<StartedEventSaga>();

    using var provider = services.BuildServiceProvider();
    provider.BuildFranzSagas();

    await using var scope = provider.CreateAsyncScope();
    var dispatcher = new FranzDispatcher(scope.ServiceProvider);
    var executer = new MessagingStrategyExecuter(
        Array.Empty<IAsyncMessageActionFilter>(),
        dispatcher);

    var act = () => executer.ExecuteAsync(message);

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*original inbound message context*");
    inboxStore.RegistrationAttempts.Should().Be(0);
    provider.GetRequiredService<InMemorySagaStateStore>().Store.Should().BeEmpty();
  }

  [Fact]
  public async Task NotificationPipeline_RejectsTransportGeneratedIdentityWhenInboxIsEnabled()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IMessagingPublisher>(new TestMessagingPublisher());
    var inboxStore = new RecordingInboxStore();
    services.AddSingleton<IInboxStore>(inboxStore);

    var message = new Message("{}");
    message.SetProperty(MessagingConstants.TransportGeneratedMessageId, true);
    message.Headers[MessagingConstants.ClassName] =
        [HeaderNamer.GetEventClassName(typeof(StartedEvent))];
    services.AddSingleton<IMessageContextAccessor>(
        new TestMessageContextAccessor(new MessageContext(message)));

    services
        .AddFranzSagas(options => options.Persistence = "Memory")
        .AddSaga<StartedEventSaga>();

    using var provider = services.BuildServiceProvider();
    provider.BuildFranzSagas();

    await using var scope = provider.CreateAsyncScope();
    var dispatcher = new FranzDispatcher(scope.ServiceProvider);
    var executer = new MessagingStrategyExecuter(
        Array.Empty<IAsyncMessageActionFilter>(),
        dispatcher);

    var act = () => executer.ExecuteAsync(message);

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*stable broker-supplied message identity*");
    inboxStore.RegistrationAttempts.Should().Be(0);
    provider.GetRequiredService<InMemorySagaStateStore>().Store.Should().BeEmpty();
  }

  [Fact]
  public async Task NotificationPipeline_DispatchesInboundIntegrationEventToRegisteredSaga()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IMessagingPublisher>(new TestMessagingPublisher());

    var messageId = Guid.CreateVersion7();
    var correlationId = Guid.CreateVersion7();
    var causationId = Guid.CreateVersion7();
    var message = new Message("{}")
    {
      Id = messageId,
      CorrelationId = correlationId
    };
    message.Headers[Message.CausationHeader] = [causationId.ToString("D")];
    message.Headers[MessagingConstants.ClassName] =
        [HeaderNamer.GetEventClassName(typeof(StartedEvent))];

    services.AddSingleton<IMessageContextAccessor>(
        new TestMessageContextAccessor(new MessageContext(message)));

    services
        .AddFranzSagas(options => options.Persistence = "Memory")
        .AddSaga<StartedEventSaga>();

    using var provider = services.BuildServiceProvider();
    provider.BuildFranzSagas();

    await using var scope = provider.CreateAsyncScope();
    var dispatcher = new FranzDispatcher(scope.ServiceProvider);

    var executer = new MessagingStrategyExecuter(
        Array.Empty<IAsyncMessageActionFilter>(),
        dispatcher);

    (await executer.CanExecuteAsync(message)).Should().BeTrue();
    await executer.ExecuteAsync(message);

    var records = provider.GetRequiredService<InMemorySagaStateStore>().Store;
    records.Should().ContainSingle();

    var persisted = records.Single().Value;
    persisted.LastMessageId.Should().Be(messageId);
    persisted.CorrelationId.Should().Be(correlationId);
    persisted.TenantId.Should().BeNull("an unvalidated tenant header must not establish tenant identity");
    persisted.Status.Should().Be(SagaLifecycleStatus.Completed);

    var state = persisted.State.Should().BeOfType<StartedEventSagaState>().Subject;
    state.CorrelationId.Should().Be(correlationId);
    state.CausationId.Should().Be(causationId);
  }

  public sealed record StartedEvent : IIntegrationEvent;

  public sealed class StartedEventSaga : SagaBase<StartedEventSagaState>, IStartWith<StartedEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        StartedEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
    {
      var state = (StartedEventSagaState)context.State;
      state.CorrelationId = context.CorrelationId;
      state.CausationId = context.CausationId;
      return Task.FromResult(context.Complete());
    }
  }

  public sealed class StartedEventSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
  }

  private sealed class TestMessageContextAccessor(IMessageContext messageContext)
      : IMessageContextAccessor
  {
    public IMessageContext? Current => messageContext;
  }

  private sealed class RecordingInboxStore : IInboxStore
  {
    public int RegistrationAttempts { get; private set; }

    public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default)
    {
      RegistrationAttempts++;
      return Task.FromResult(true);
    }

    public Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
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
