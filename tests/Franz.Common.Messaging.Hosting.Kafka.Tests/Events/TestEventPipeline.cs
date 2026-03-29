#nullable enable
using Franz.Common.Mediator.Validation.Events;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Handlers;

/// <summary>
/// Integration test pipeline to verify middleware execution.
/// </summary>
public sealed class TestEventPipeline<TEvent> : IEventPipeline<TEvent>
    where TEvent : class, IEvent
{
  private readonly ITestPipelineProbe _probe;
  private readonly ILogger<TestEventPipeline<TEvent>> _logger;

  public TestEventPipeline(ITestPipelineProbe probe, ILogger<TestEventPipeline<TEvent>> logger)
  {
    _probe = probe;
    _logger = logger;
  }

  public async Task HandleAsync(TEvent @event, Func<Task> next, CancellationToken ct = default)
  {
    // 🔍 Correlation: Attempt to track via Message ID if the event is a transport message
    var messageId = (@event as Message)?.Id ?? Guid.Empty;

    _logger.LogDebug("🧪 Pipeline: Processing {EventName} (ID: {MessageId})", typeof(TEvent).Name, messageId);

    try
    {
      // Execute the next step in the pipeline (or the final Handler)
      await next();

      _logger.LogDebug("✅ Pipeline: Completed {MessageId}", messageId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "❌ Pipeline: Failed {MessageId}", messageId);
      throw;
    }
  }
}