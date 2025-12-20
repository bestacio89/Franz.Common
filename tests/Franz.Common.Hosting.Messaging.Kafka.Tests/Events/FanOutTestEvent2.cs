using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;

public sealed record FanoutTestEvent2(string Value) : IIntegrationEvent
{
  public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
