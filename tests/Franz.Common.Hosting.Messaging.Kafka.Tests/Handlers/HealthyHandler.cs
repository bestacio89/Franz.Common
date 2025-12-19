namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;

using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;
using Franz.Common.Mediator.Handlers;

public sealed class HealthyHandler : IEventHandler<FaultToleranceTestEvent>
{
  public Task HandleAsync(FaultToleranceTestEvent evt, CancellationToken ct)
  {
    FaultToleranceProbe.Hit(evt.Value);
    return Task.CompletedTask;
  }
}
