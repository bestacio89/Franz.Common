namespace Franz.Common.Mediator.Messages;

/// <summary>A query that returns a value of type <typeparamref name="TResponse"/>.</summary>
public interface IQuery<TResponse> : IQueryMessage
{
}
