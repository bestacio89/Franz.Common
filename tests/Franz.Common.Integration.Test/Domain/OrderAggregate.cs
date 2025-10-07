using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.IntegrationTesting.Domain.Events;
using Franz.Common.Mediator.Messages;

namespace Franz.Common.IntegrationTesting.Domain;

public sealed class OrderAggregate : AggregateRoot<IDomainEvent>
{
  public Guid OrderId { get; private set; }
  public Guid CustomerId { get; private set; }
  public bool IsCancelled { get; private set; }

  private readonly List<OrderLine> _lines = new();
  public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
  public decimal Total => _lines.Sum(l => l.LineTotal);

  // Default constructor ensures handlers are always registered
  public OrderAggregate()
  {
    RegisterHandlers();
  }

  // Constructor for rehydration, also ensures handlers registered
  private OrderAggregate(Guid id) : base(id)
  {
    RegisterHandlers();
  }

  public static OrderAggregate CreateNew(Guid orderId, Guid customerId, IEnumerable<OrderLine> lines)
  {
    var agg = new OrderAggregate();

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
    _lines.AddRange(e.Lines.Select(l =>
        new OrderLine(l.Sku, l.Quantity, l.UnitPrice)
    ));
    IsCancelled = false;
  }

  private void Apply(OrderCancelledEvent _)
  {
    IsCancelled = true;
  }

  public static OrderAggregate Rehydrate(Guid id, IEnumerable<IEvent> history)
  {
    var agg = new OrderAggregate(id);
    agg.ReplayEvents((IEnumerable<IDomainEvent>)history); // inherited from AggregateRoot
    return agg;
  }
}
