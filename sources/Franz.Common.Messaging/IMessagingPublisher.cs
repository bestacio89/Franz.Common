#nullable enable
using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging;

/// <summary>
/// Publishes an integration event through the Franz mediator pipeline 
/// and then forwards it to the external messaging system (e.g., RabbitMQ/Kafka).
/// </summary>
public interface IMessagingPublisher
{
  /// <summary>
  /// Publishes an integration event.
  /// Senior Note: Added CancellationToken to honor .NET 10 async standards.
  /// </summary>
  ValueTask Publish<TIntegrationEvent>(
      TIntegrationEvent integrationEvent,
      CancellationToken ct = default)
      where TIntegrationEvent : IIntegrationEvent;
}