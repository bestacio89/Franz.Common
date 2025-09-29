using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.IntegrationTesting.Commands;
public sealed class CancelOrderCommand : ICommand<Unit>
{
  public Guid OrderId { get; init; }
}
