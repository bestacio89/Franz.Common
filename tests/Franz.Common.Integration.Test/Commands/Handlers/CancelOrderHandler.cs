using Franz.Common.Business.Events;
using Franz.Common.Integration.Tests.Commands;
using Franz.Common.IntegrationTesting.Domain;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;

public sealed class CancelOrderHandler : ICommandHandler<CancelOrderCommand, Unit>
{
  private readonly IAggregateRootRepository<OrderAggregate, IDomainEvent> _repository;

  public CancelOrderHandler(IAggregateRootRepository<OrderAggregate, IDomainEvent> repository)
      => _repository = repository;

  public async Task<Unit> Handle(CancelOrderCommand command, CancellationToken ct = default)
  {
    // no need to expose generics here anymore
    var agg = OrderAggregate.Rehydrate(command.OrderId, Array.Empty<IEvent>());

    agg.Cancel();

    await _repository.SaveAsync(agg, ct);

    return Unit.Value;
  }

}
