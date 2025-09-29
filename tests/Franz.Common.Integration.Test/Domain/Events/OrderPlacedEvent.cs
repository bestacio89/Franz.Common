using Franz.Common.Business.Domain;
using Franz.Common.IntegrationTesting.Domain;
using Franz.Common.IntegrationTesting.Domain.Events;
using Franz.Common.Mediator.Messages;
using System.Diagnostics;

[DebuggerDisplay("OrderPlaced (Aggregate={AggregateId}, Total={Total})")]
public sealed class OrderPlacedEvent : BaseDomainEvent, INotification
{
  public Guid CustomerId { get; }
  public List<OrderLineDto> Lines { get; }
  public decimal Total { get; }

  public OrderPlacedEvent(Guid aggregateId, Guid customerId, List<OrderLineDto> lines, decimal total, string? correlationId = null)
      : base(aggregateId, nameof(OrderAggregate), correlationId)
  {
    CustomerId = customerId;
    Lines = lines ?? new List<OrderLineDto>();
    Total = total;
  }
}
