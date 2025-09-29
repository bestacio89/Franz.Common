using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;

namespace Franz.Common.IntegrationTesting.Commands;

public sealed class PlaceOrderCommand : ICommand<Unit>
{
  public Guid OrderId { get; init; } = Guid.NewGuid();
  public Guid CustomerId { get; init; }
  public List<(string sku, int qty, decimal price)> Lines { get; init; } = new();
}
