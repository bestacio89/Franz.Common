using Franz.Common.Mediator.Handlers;

namespace Franz.Common.Business.AotSmokeTest;

public sealed class AotCommandHandler : ICommandHandler<AotCommand, string>
{
  public Task<string> Handle(AotCommand command, CancellationToken cancellationToken)
      => Task.FromResult(command.Value);
}