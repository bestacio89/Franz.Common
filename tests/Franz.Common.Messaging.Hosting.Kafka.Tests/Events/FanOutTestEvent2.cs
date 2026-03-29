using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Events;

public sealed record FanoutTestEvent2(string Value) : IIntegrationEvent
{
  public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
