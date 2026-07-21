using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Tests.Fixtures;

public record PingCommand(string Message) : ICommand<string>;

public sealed class PingCommandHandler : ICommandHandler<PingCommand, string>
{
  public Task<string> Handle(PingCommand command, CancellationToken cancellationToken)
  {
    return Task.FromResult($"Pong: {command.Message}");
  }
}