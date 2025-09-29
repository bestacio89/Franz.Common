using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Messages;
using System.Diagnostics;

namespace Franz.Common.IntegrationTesting.Domain.Events;

[DebuggerDisplay("OrderCancelled (Aggregate={AggregateId})")]
public sealed class OrderCancelledEvent : BaseDomainEvent, INotification
{
  /// <summary>
  /// Reason for cancellation (optional).
  /// </summary>
  public string? Reason { get; }

  public OrderCancelledEvent(Guid aggregateId, string? reason = null, string? correlationId = null)
      : base(aggregateId, nameof(OrderAggregate), correlationId)
  {
    Reason = reason;
  }
}
