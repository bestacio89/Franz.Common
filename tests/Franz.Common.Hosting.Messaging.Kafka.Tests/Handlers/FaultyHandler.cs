namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;

using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Mediator.Messages;

public sealed class FaultyHandler
  : INotificationHandler<FaultToleranceTestEvent>
{
  public Task Handle(FaultToleranceTestEvent evt, CancellationToken ct)
    => throw new InvalidOperationException("💥 handler exploded");
}
