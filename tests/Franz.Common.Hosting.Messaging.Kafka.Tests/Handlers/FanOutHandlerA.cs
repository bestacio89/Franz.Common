using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;
using Franz.Common.Mediator.Handlers;

public sealed class FanoutHandlerA : IEventHandler<FanoutTestEvent>
{
  public Task HandleAsync(FanoutTestEvent evt, CancellationToken ct)
  {
    MultiHandlerProbe.Hit(evt.Value);
    return Task.CompletedTask;
  }
}