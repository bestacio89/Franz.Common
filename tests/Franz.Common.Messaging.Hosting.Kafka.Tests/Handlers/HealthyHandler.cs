namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Handlers;

using Franz.Common.Messaging.Hosting.Kafka.Tests.Events;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
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
