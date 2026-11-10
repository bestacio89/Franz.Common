#nullable enable

using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Registration;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Messages;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using static Franz.Common.Mediator.Dispatchers.DispatchingStrategies;

namespace Franz.Common.Messaging.Sagas.Tests.Core;

public sealed class SagaRetryDispatchTests
{
  static SagaRetryDispatchTests()
  {
    GeneratedMediatorInvokerRegistry.RegisterNotification<RetryEvent>();
    GeneratedMediatorInvokerRegistry.RegisterNotification<SagaRetryRequested>();
  }

  [Fact]
  public async Task RetryDelivery_RehydratesPersistedSagaWithoutPublishingBusinessEvent()
  {
    var serializer = new JsonMessageSerializer();
    var sagaId = Guid.CreateVersion7();
    var originalMessageId = Guid.CreateVersion7();
    var retryMessageId = Guid.CreateVersion7();
    var correlationId = Guid.CreateVersion7();

    var request = new SagaRetryRequested
    {
      SagaId = sagaId,
      OriginalMessageId = originalMessageId,
      OriginalMessageType = typeof(RetryEvent).AssemblyQualifiedName!,
      Body = serializer.Serialize(new RetryEvent()),
      Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
      {
        [Message.CorrelationHeader] = [correlationId.ToString("D")],
        [Message.CausationHeader] = [originalMessageId.ToString("D")],
        ["x-original-trace"] = ["trace-1"]
      },
      Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
      OccurredOn = DateTimeOffset.UtcNow
    };

    var retryMessage = new Message(serializer.Serialize(request))
    {
      Id = retryMessageId,
      CorrelationId = correlationId,
      MessageType = typeof(SagaRetryRequested).AssemblyQualifiedName
    };
    retryMessage.Headers[Message.CorrelationHeader] = [correlationId.ToString("D")];
    retryMessage.Headers[Message.CausationHeader] = [originalMessageId.ToString("D")];
    retryMessage.Headers[SagaRetryHeaders.TargetSagaId] = [sagaId.ToString("D")];

    var businessEventObserver = new BusinessEventObserver();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IMessagingPublisher, TestMessagingPublisher>();
    services.AddSingleton<IMessageSerializer>(serializer);
    services.AddSingleton<IMessageContextAccessor>(
        new TestMessageContextAccessor(new MessageContext(retryMessage)));
    services.AddFranzSagas(options => options.Persistence = "Memory")
        .AddSaga<RetrySaga>();
    services.AddSingleton(businessEventObserver);
    services.AddScoped<INotificationHandler<RetryEvent>>(
        serviceProvider => serviceProvider.GetRequiredService<BusinessEventObserver>());

    using var provider = services.BuildServiceProvider();
    provider.BuildFranzSagas();

    await using var scope = provider.CreateAsyncScope();
    var repository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();
    var now = DateTimeOffset.UtcNow;
    await repository.SaveAsync(new SagaPersistenceRecord
    {
      SagaId = sagaId,
      CorrelationId = correlationId,
      SagaType = typeof(RetrySaga).AssemblyQualifiedName!,
      State = new RetrySagaState { Attempts = 1 },
      Status = SagaLifecycleStatus.RetryScheduled,
      RetryCount = 1,
      LastMessageId = retryMessageId,
      NextAttemptAt = now.AddSeconds(-1),
      CreatedAt = now,
      UpdatedAt = now
    }, expectedVersion: null);

    var dispatcher = new FranzDispatcher(scope.ServiceProvider);
    await dispatcher.PublishNotificationAsync(
        (INotification)request,
        CancellationToken.None,
        PublishStrategy.Sequential,
        NotificationErrorHandling.StopOnFirstFailure);

    var persisted = await repository.LoadAsync(sagaId, null, typeof(RetrySagaState));
    persisted.Should().NotBeNull();
    persisted!.Status.Should().Be(SagaLifecycleStatus.Completed);
    persisted.RetryCount.Should().Be(0);
    persisted.NextAttemptAt.Should().BeNull();
    ((RetrySagaState)persisted.State).Attempts.Should().Be(2);
    businessEventObserver.Received.Should().Be(0);
  }

  [Fact]
  public async Task RetryDelivery_RejectsMismatchedOriginalCausationIdentity()
  {
    var serializer = new JsonMessageSerializer();
    var sagaId = Guid.CreateVersion7();
    var originalMessageId = Guid.CreateVersion7();
    var retryMessageId = Guid.CreateVersion7();
    var correlationId = Guid.CreateVersion7();

    var request = new SagaRetryRequested
    {
      SagaId = sagaId,
      OriginalMessageId = originalMessageId,
      OriginalMessageType = typeof(RetryEvent).AssemblyQualifiedName!,
      Body = serializer.Serialize(new RetryEvent()),
      Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
      {
        [Message.CorrelationHeader] = [correlationId.ToString("D")],
        [Message.CausationHeader] = [Guid.CreateVersion7().ToString("D")]
      },
      Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
      OccurredOn = DateTimeOffset.UtcNow
    };

    var retryMessage = new Message(serializer.Serialize(request))
    {
      Id = retryMessageId,
      CorrelationId = correlationId,
      MessageType = typeof(SagaRetryRequested).AssemblyQualifiedName
    };
    retryMessage.Headers[Message.CorrelationHeader] = [correlationId.ToString("D")];
    retryMessage.Headers[Message.CausationHeader] = [originalMessageId.ToString("D")];
    retryMessage.Headers[SagaRetryHeaders.TargetSagaId] = [sagaId.ToString("D")];

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IMessagingPublisher, TestMessagingPublisher>();
    services.AddSingleton<IMessageSerializer>(serializer);
    services.AddSingleton<IMessageContextAccessor>(
        new TestMessageContextAccessor(new MessageContext(retryMessage)));
    services.AddFranzSagas(options => options.Persistence = "Memory")
        .AddSaga<RetrySaga>();

    using var provider = services.BuildServiceProvider();
    provider.BuildFranzSagas();

    await using var scope = provider.CreateAsyncScope();
    var dispatcher = new FranzDispatcher(scope.ServiceProvider);

    var act = () => dispatcher.PublishNotificationAsync(
        (INotification)request,
        CancellationToken.None,
        PublishStrategy.Sequential,
        NotificationErrorHandling.StopOnFirstFailure);

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*original envelope causation*");
  }

  public sealed record RetryEvent : IIntegrationEvent;

  public sealed class RetrySaga : SagaBase<RetrySagaState>, IStartWith<RetryEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        RetryEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
    {
      var state = (RetrySagaState)context.State;
      state.Attempts++;
      return Task.FromResult(context.Complete());
    }
  }

  public sealed class RetrySagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
    public int Attempts { get; set; }
  }

  private sealed class BusinessEventObserver : INotificationHandler<RetryEvent>
  {
    public int Received { get; private set; }

    public Task Handle(RetryEvent notification, CancellationToken cancellationToken = default)
    {
      Received++;
      return Task.CompletedTask;
    }
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
