using Franz.Common.Mediator.Messages;

public sealed record FanoutTestEvent(string Value) : IEvent
{
  public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
