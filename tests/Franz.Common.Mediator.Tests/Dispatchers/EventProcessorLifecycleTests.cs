using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Events.PostProcessing;
using Franz.Common.Mediator.Pipelines.Events.Preprocessing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Mediator.Tests.Dispatchers;

public sealed class EventProcessorLifecycleTests
{
  public sealed record TestEvent : IEvent;

  public sealed class RecordingHandler(List<string> calls, bool shouldFail = false) : IEventHandler<TestEvent>
  {
    public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
    {
      calls.Add("handler");
      return shouldFail
          ? Task.FromException(new InvalidOperationException("handler failed"))
          : Task.CompletedTask;
    }
  }

  public sealed class RecordingPreProcessor(List<string> calls) : IEventPreProcessor<TestEvent>
  {
    public Task ProcessAsync(TestEvent @event, CancellationToken ct = default)
    {
      calls.Add("pre");
      return Task.CompletedTask;
    }
  }

  public sealed class RecordingPostProcessor(List<string> calls) : IEventPostProcessor<TestEvent>
  {
    public Task ProcessAsync(TestEvent @event, CancellationToken ct = default)
    {
      calls.Add("post");
      return Task.CompletedTask;
    }
  }

  [Fact]
  public async Task PublishEventAsync_ExecutesPreProcessorHandlerAndPostProcessorInOrder()
  {
    var calls = new List<string>();
    var services = new ServiceCollection();
    services.AddSingleton<IEventPreProcessor<TestEvent>>(new RecordingPreProcessor(calls));
    services.AddSingleton<IEventHandler<TestEvent>>(new RecordingHandler(calls));
    services.AddSingleton<IEventPostProcessor<TestEvent>>(new RecordingPostProcessor(calls));
    using var provider = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(provider);

    await dispatcher.PublishEventAsync(new TestEvent());

    Assert.Equal(["pre", "handler", "post"], calls);
  }

  [Fact]
  public async Task PublishEventAsync_WhenHandlerFails_DoesNotRunPostProcessors()
  {
    var calls = new List<string>();
    var services = new ServiceCollection();
    services.AddSingleton<IEventPreProcessor<TestEvent>>(new RecordingPreProcessor(calls));
    services.AddSingleton<IEventHandler<TestEvent>>(new RecordingHandler(calls, shouldFail: true));
    services.AddSingleton<IEventPostProcessor<TestEvent>>(new RecordingPostProcessor(calls));
    using var provider = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(provider);

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => dispatcher.PublishEventAsync(new TestEvent()));

    Assert.Equal(["pre", "handler"], calls);
  }
}
