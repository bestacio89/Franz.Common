using Franz.Common.Mediator.Dispatchers;

namespace Franz.Common.Mediator.Messages;

/// <summary>A command that explicitly returns the mediator's unit value.</summary>
public interface ICommandNoResponse : ICommand<Unit>
{
}
