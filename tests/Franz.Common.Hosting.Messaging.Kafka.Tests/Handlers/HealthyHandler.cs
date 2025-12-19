namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;

using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;
using Franz.Common.Mediator.Messages;

public sealed class HealthyHandler
  : INotificationHandler<FaultToleranceTestEvent>
{
  public Task Handle(FaultToleranceTestEvent evt, CancellationToken ct)
  {
    FaultToleranceProbe.Hit(evt.Value);
    return Task.CompletedTask;
  }
}
