namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;

using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Mediator.Handlers;

public sealed class FaultyHandler : IEventHandler<FaultToleranceTestEvent>
{
  public Task HandleAsync(FaultToleranceTestEvent evt, CancellationToken ct)
    => throw new InvalidOperationException("💥 handler exploded");
}
