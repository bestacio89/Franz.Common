#nullable enable
using Franz.Common.Mediator.Handlers;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Events;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Handlers;

/// <summary>
/// Final consumer in the Mediator pipeline for integration testing.
/// Signals the TestProbe to notify the XUnit thread of successful processing.
/// </summary>
public sealed class TestEventHandler : IEventHandler<TestEvent>
{
  private readonly ITestProbe _probe;
  private readonly ILogger<TestEventHandler> _logger;

  public TestEventHandler(ITestProbe probe, ILogger<TestEventHandler> logger)
  {
    _probe = probe;
    _logger = logger;
  }

  public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("🎯 Handling TestEvent {MessageId} for {Name}", @event.Id, @event.Value);

    try
    {
      // ⚡ The critical link: Signal the probe using the GuidV7 ID
      _probe.SignalArrival(@event.Id);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "❌ Failed to signal probe for message {MessageId}", @event.Id);
      // We signal failure so the test wakes up with the actual error instead of timing out
      _probe.SignalFailure(@event.Id, ex);
    }

    return Task.CompletedTask;
  }
}