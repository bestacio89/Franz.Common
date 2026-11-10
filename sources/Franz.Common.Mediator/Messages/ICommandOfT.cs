namespace Franz.Common.Mediator.Messages;

/// <summary>A command that returns a value of type <typeparamref name="TResponse"/>.</summary>
public interface ICommand<TResponse> : ICommandMessage
{
}
