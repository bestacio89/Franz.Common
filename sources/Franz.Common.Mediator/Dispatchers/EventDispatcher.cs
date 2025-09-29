using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Validation.Events;
using Microsoft.Extensions.DependencyInjection;
using static Franz.Common.Mediator.Dispatchers.DispatchingStrategies;

public sealed class EventDispatcher : IEventDispatcher
{
  private readonly IServiceProvider _services;

  public EventDispatcher(IServiceProvider services)
  {
    _services = services;
  }

  public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
      where TEvent : IEvent
  {
    // Resolve all handlers for this event
    var handlers = _services.GetServices<IEventHandler<TEvent>>().ToList();

    // Resolve any pipelines (like your EventValidationPipeline)
    var pipelines = _services.GetServices<IEventPipeline<TEvent>>().ToList();

    // Build the execution chain
    Func<Task> pipelineChain = () =>
    {
      var tasks = handlers.Select(h => h.HandleAsync(@event, cancellationToken));
      return Task.WhenAll(tasks);
    };

    foreach (var pipeline in pipelines.AsEnumerable().Reverse())
    {
      var next = pipelineChain;
      pipelineChain = () => pipeline.HandleAsync(@event, next, cancellationToken);
    }

    await pipelineChain();
  }
}
