using Franz.Common.Mediator.Messages;


namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Events;

public sealed record FanoutTestEvent(string Value) : IEvent
{
  public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
