using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Events;

public sealed class FanoutHandlerA : IEventHandler<FanoutTestEvent>
{
  public Task HandleAsync(FanoutTestEvent evt, CancellationToken ct)
  {
    MultiHandlerProbe.MarkHandled();
    return Task.CompletedTask;
  }
}