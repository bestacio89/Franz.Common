using Castle.Core.Resource;
using Franz.Common.IntegrationTesting.Commands;
using Franz.Common.IntegrationTesting.Domain;
using Franz.Common.IntegrationTesting.Domain.Events;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Moq;

public sealed class PlaceOrderHandler : ICommandHandler<PlaceOrderCommand, Unit>
{
  private readonly IEventDispatcher _dispatcher;

  public PlaceOrderHandler(IEventDispatcher dispatcher)
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
    foreach (var ev in order.GetUncommittedChanges())
    {
      await _dispatcher.PublishAsync(ev, cancellationToken);
    }
    
    order.MarkChangesAsCommitted();

    return Unit.Value;
  }
}
