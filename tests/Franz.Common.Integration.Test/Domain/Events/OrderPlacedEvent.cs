using Franz.Common.Business.Events;
using Franz.Common.Mediator.Messages;
using System.Diagnostics;

namespace Franz.Common.IntegrationTesting.Domain.Events;

[DebuggerDisplay("OrderPlaced (Aggregate={AggregateId}, Total={Total})")]
public sealed class OrderPlacedEvent : IDomainEvent
{
  public OrderPlacedEvent(Guid aggregateId, Guid customerId, List<OrderLineDto> lines, decimal total, string? correlationId = null)
  {
    EventId = Guid.NewGuid();
    OccurredOn = DateTimeOffset.UtcNow;
    CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    AggregateId = aggregateId;
    AggregateType = nameof(OrderAggregate);
    CustomerId = customerId;
    Lines = lines ?? new List<OrderLineDto>();
    Total = total;
  }

  /// <summary>
  /// Unique identifier for this event instance (useful for deduplication/outbox).
  /// </summary>
  public Guid EventId { get; }

  /// <summary>
  /// When the event was created (in UTC).
  /// </summary>
  public DateTimeOffset OccurredOn { get; }

  /// <summary>
  /// Correlation/trace identifier (for distributed tracing).
  /// </summary>
  public string? CorrelationId { get; }

  /// <summary>
  /// Aggregate root identifier that raised this event.
  /// </summary>
  public Guid? AggregateId { get; }

  /// <summary>
  /// Type of aggregate root (e.g. "Order", "Customer").
  /// </summary>
  public string AggregateType { get; }

  /// <summary>
  /// Event type name (by default, the class name).
  /// Useful for serialization/logging.
  /// </summary>
  public string EventType => GetType().Name;

  /// <summary>
  /// Id of the customer who placed the order.
  /// </summary>
  public Guid CustomerId { get; }

  /// <summary>
  /// Order lines.
  /// </summary>
  public List<OrderLineDto> Lines { get; }

  /// <summary>
  /// Total order value.
  /// </summary>
  public decimal Total { get; }
}
