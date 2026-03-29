namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Handlers;

using Franz.Common.Messaging.Hosting.Kafka.Tests.Events;
using Franz.Common.Mediator.Messages;

public sealed class FaultyHandler
  : INotificationHandler<FaultToleranceTestEvent>
{
  public Task Handle(FaultToleranceTestEvent evt, CancellationToken ct)
    => throw new InvalidOperationException("💥 handler exploded");
}
