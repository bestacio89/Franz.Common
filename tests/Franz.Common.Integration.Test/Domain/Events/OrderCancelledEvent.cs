using Franz.Common.Business.Events;
using Franz.Common.IntegrationTesting.Domain;

using System.Diagnostics;

[DebuggerDisplay("OrderCancelled (Aggregate={AggregateId}, Reason={Reason})")]
public sealed class OrderCancelledEvent : IDomainEvent
{
  public OrderCancelledEvent(Guid aggregateId, string? reason = null, string? correlationId = null)
  {
    EventId = Guid.NewGuid();
    OccurredOn = DateTimeOffset.UtcNow;
    CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    AggregateId = aggregateId;
    AggregateType = nameof(OrderAggregate);
    Reason = reason;
  }

  public Guid EventId { get; }
  public DateTimeOffset OccurredOn { get; }
  public string? CorrelationId { get; }
  public Guid? AggregateId { get; }
  public string AggregateType { get; }
  public string EventType => GetType().Name;

  public string? Reason { get; }
}
