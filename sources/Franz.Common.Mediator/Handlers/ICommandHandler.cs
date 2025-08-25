using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Handlers;
// A handler for a command with no response
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
  Task Handle(TCommand command, CancellationToken cancellationToken);
}

// A handler for a command that returns a response
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
  Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}