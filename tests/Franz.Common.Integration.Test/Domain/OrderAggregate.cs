using Franz.Common.Business.Domain;
using Franz.Common.IntegrationTesting.Domain.Events;

namespace Franz.Common.IntegrationTesting.Domain;

public sealed class OrderAggregate : AggregateRoot<BaseDomainEvent>
{
  public Guid OrderId { get; private set; }
  public Guid CustomerId { get; private set; }
  public bool IsCancelled { get; private set; }
  private readonly List<OrderLine> _lines = new();

  public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
  public decimal Total => _lines.Sum(l => l.LineTotal);

  private OrderAggregate() { }

  public static OrderAggregate CreateNew(Guid orderId, Guid customerId, IEnumerable<OrderLine> lines)
  {
    var agg = new OrderAggregate();
    agg.RegisterHandlers();

    agg.RaiseEvent(new OrderPlacedEvent(
        orderId,
        customerId,
        lines.Select(l => new OrderLineDto(l.Sku, l.Quantity, l.UnitPrice)).ToList(),
        lines.Sum(l => l.LineTotal)
    ));

    return agg;
  }

  public void Cancel(string? reason = null)
  {
    if (IsCancelled) return;

    RaiseEvent(new OrderCancelledEvent(
        OrderId,
        reason
    ));
  }

  private void RegisterHandlers()
  {
    Register<OrderPlacedEvent>(Apply);
    Register<OrderCancelledEvent>(Apply);
  }

  private void Apply(OrderPlacedEvent e)
  {
    OrderId = e.AggregateId ?? Guid.Empty;
    CustomerId = e.CustomerId;
    _lines.Clear();
    _lines.AddRange(e.Lines.Select(l => new OrderLine(l.Sku, l.Quantity, l.UnitPrice)));
  }

  private void Apply(OrderCancelledEvent _)
  {
    IsCancelled = true;
  }

  public static OrderAggregate Rehydrate(Guid orderId, Guid customerId)
  {
    var agg = new OrderAggregate();
    agg.RegisterHandlers();
    agg.OrderId = orderId;
    agg.CustomerId = customerId;
    return agg;

  }

}
