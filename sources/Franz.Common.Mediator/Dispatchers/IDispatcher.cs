using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Dispatchers;
public interface IDispatcher
{
  // Overload for commands with a response
  Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

  // Overload for commands with no response
  Task Send(ICommand command, CancellationToken cancellationToken = default);

  // Overload for queries
  Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
