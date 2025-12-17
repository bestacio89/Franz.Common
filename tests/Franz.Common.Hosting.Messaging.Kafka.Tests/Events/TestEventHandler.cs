using Franz.Common.Mediator.Handlers;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;

public sealed class TestEventHandler : IEventHandler<TestEvent>
{
  private readonly ITestProbe _probe;

  public TestEventHandler(ITestProbe probe)
  {
    _probe = probe;
  }
  public static TaskCompletionSource<TestEvent> Received { get; private set; }
    = Create();

  private static TaskCompletionSource<TestEvent> Create()
    => new(TaskCreationOptions.RunContinuationsAsynchronously);

  public static void Reset()
    => Received = Create();

  public Task HandleAsync(TestEvent notification, CancellationToken cancellationToken)
  {
    Received.TrySetResult(notification);
    _probe.MarkHandled();
    return Task.CompletedTask;
  }
}
