using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Validation.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Events;

public class TestEventPipeline<TEvent> : IEventPipeline<TEvent>
    where TEvent : IEvent
{
  private readonly ITestPipelineProbe _probe;

  public TestEventPipeline(ITestPipelineProbe probe)
  {
    _probe = probe;
  }

  public async Task HandleAsync(TEvent @event, Func<Task> next, CancellationToken ct)
  {
    // 1. Mark that we entered the pipeline
    _probe.MarkExecuted();

    // 2. Continue the chain (crucial for your recursive handlerChain)
    await next();
  }
}