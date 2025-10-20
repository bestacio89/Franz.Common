using Castle.Core.Resource;
using Franz.Common.Integration.Tests.Mediator.Commands;
using Franz.Common.Integration.Tests.Mediator.Domain;
using Franz.Common.Integration.Tests.Mediator.Domain.Events;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Moq;

public sealed class PlaceOrderHandler : ICommandHandler<PlaceOrderCommand, Unit>
{
  private readonly IDispatcher _dispatcher;

  public PlaceOrderHandler(IDispatcher dispatcher)
  {
    _dispatcher = dispatcher;
  }

  public async Task<Unit> Handle(PlaceOrderCommand command, CancellationToken cancellationToken = default)
  {
    var order = OrderAggregate.CreateNew(
        command.OrderId,
        command.CustomerId,
        command.Lines.Select(l => new OrderLine(l.sku, l.qty, unitPrice: l.price))
    );


    // 🚀 Publish all raised events
    foreach (var ev in order.GetUncommittedChanges().Cast<OrderPlacedEvent>())
    {
      await _dispatcher.PublishEventAsync(ev, cancellationToken);
    }


    order.MarkChangesAsCommitted();

    return Unit.Value;
  }
}
