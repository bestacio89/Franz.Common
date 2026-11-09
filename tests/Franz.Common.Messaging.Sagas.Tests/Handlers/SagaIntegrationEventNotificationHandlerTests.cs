#nullable enable

using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Hosting.Delegating;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Messaging.Sagas.Tests.Handlers;

public sealed class SagaIntegrationEventNotificationHandlerTests
{
  [Fact]
  public async Task NotificationPipeline_DispatchesInboundIntegrationEventToRegisteredSaga()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IMessagingPublisher, TestMessagingPublisher>();

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
    persisted.Status.Should().Be(SagaLifecycleStatus.Completed);
  }

  public sealed record StartedEvent : IIntegrationEvent;

  public sealed class StartedEventSaga : SagaBase<StartedEventSagaState>, IStartWith<StartedEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        StartedEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Complete());
  }

  public sealed class StartedEventSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed class TestMessageContextAccessor(IMessageContext messageContext)
      : IMessageContextAccessor
  {
    public IMessageContext? Current => messageContext;
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
