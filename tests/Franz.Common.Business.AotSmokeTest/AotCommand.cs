using Franz.Common.Mediator.Messages;

namespace Franz.Common.Business.AotSmokeTest;

public sealed record AotCommand(string Value) : ICommand<string>;