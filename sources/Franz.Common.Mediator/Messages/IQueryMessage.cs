namespace Franz.Common.Mediator.Messages;

/// <summary>
/// Non-generic marker for query requests. Enables query classification without runtime interface inspection.
/// </summary>
public interface IQueryMessage : IMessage
{
}
