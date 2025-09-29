using Franz.Common.IntegrationTesting.Domain;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.IntegrationTesting.Commands.Handlers;
public sealed class CancelOrderHandler : ICommandHandler<CancelOrderCommand, Unit>
{
  private readonly IEventDispatcher _dispatcher;

  public CancelOrderHandler(IEventDispatcher dispatcher) => _dispatcher = dispatcher;



  // For the test we create a minimal agg instance and raise cancel
  public async Task<Unit> Handle(CancelOrderCommand command, CancellationToken ct = default)
  {
    var agg = OrderAggregate.Rehydrate(command.OrderId, Guid.Empty); // no customer in this test
    agg.Cancel();

    foreach (var ev in agg.GetUncommittedChanges())
      await _dispatcher.PublishAsync(ev, ct);

    agg.MarkChangesAsCommitted();
    return Unit.Value;
  }

}
