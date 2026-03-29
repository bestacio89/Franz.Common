#nullable enable
using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Factories;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka;

/// <summary>
/// Senior Note: Using Primary Constructor for clean dependency injection.
/// Sealed for JIT devirtualization and performance optimization on the hot path.
/// </summary>
public sealed class MessagingPublisher(
    IMessagingInitializer messagingInitializer,
    IMessageFactory messageFactory,
    IDispatcher dispatcher,
    IMessagingSender sender) : IMessagingPublisher
{
  public async ValueTask Publish<TIntegrationEvent>(
      TIntegrationEvent integrationEvent,
      CancellationToken ct = default)
      where TIntegrationEvent : IIntegrationEvent
  {
    // .NET 10 optimized guard
    ArgumentNullException.ThrowIfNull(integrationEvent);

    // 1. Ensure Kafka infra exists (topics, DLQ, etc.) 
    // Propagating CT to handle potential infrastructure timeouts.
    await messagingInitializer.InitializeAsync(ct);

    // 2. Build Franz message (Transform domain event to transport envelope)
    var message = messageFactory.Build(integrationEvent);

    // 3. Run mediator pipeline (Internal notifications/Audit)
    // Senior Note: Await here ensures internal side-effects complete before external publish.
    await dispatcher.PublishNotificationAsync(message, ct);

    // 4. Delegate transport publishing
    // Async v7+ compliant: honors cancellation during the Kafka Produce call.
    await sender.SendAsync(message, ct);
  }
}