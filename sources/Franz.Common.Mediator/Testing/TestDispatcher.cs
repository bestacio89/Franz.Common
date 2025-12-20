using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Testing
{
  /// <summary>
  /// A lightweight dispatcher for testing handlers without DI.
  /// </summary>
  public class TestDispatcher : IDispatcher
  {
    private readonly Dictionary<Type, object> _handlers = new();
    private readonly List<object> _pipelines = new();
    private readonly List<object> _preProcessors = new();
    private readonly List<object> _postProcessors = new();
    private readonly List<object> _notificationPipelines = new();
    private readonly List<object> _notificationHandlers = new();

    // -------------------- CONFIGURATION --------------------

    public TestDispatcher WithHandler<TRequest, TResponse>(ICommandHandler<TRequest, TResponse> handler)
        where TRequest : ICommand<TResponse>
    {
      _handlers[typeof(TRequest)] = handler;
      return this;
    }

    public TestDispatcher WithHandler<TRequest>(ICommandHandler<TRequest, Unit> handler)
        where TRequest : ICommand<Unit>
    {
      _handlers[typeof(TRequest)] = handler;
      return this;
    }

    public TestDispatcher WithHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> handler)
        where TQuery : IQuery<TResponse>
    {
      _handlers[typeof(TQuery)] = handler;
      return this;
    }

    public TestDispatcher WithHandler<TNotification>(INotificationHandler<TNotification> handler)
        where TNotification : INotification
    {
      _notificationHandlers.Add(handler);
      return this;
    }

    public TestDispatcher WithHandler<TQuery, TResponse>(IStreamQueryHandler<TQuery, TResponse> handler)
        where TQuery : IStreamQuery<TResponse>
    {
      _handlers[typeof(TQuery)] = handler;
      return this;
    }

    public TestDispatcher WithPipeline<TRequest, TResponse>(IPipeline<TRequest, TResponse> pipeline)
    {
      _pipelines.Add(pipeline);
      return this;
    }

    public TestDispatcher WithPreProcessor<TRequest>(IPreProcessor<TRequest> processor)
    {
      _preProcessors.Add(processor);
      return this;
    }

    public TestDispatcher WithPostProcessor<TRequest, TResponse>(IPostProcessor<TRequest, TResponse> processor)
    {
      _postProcessors.Add(processor);
      return this;
    }

    public TestDispatcher WithNotificationPipeline<TNotification>(INotificationPipeline<TNotification> pipeline)
        where TNotification : INotification
    {
      _notificationPipelines.Add(pipeline);
      return this;
    }

    // -------------------- COMMANDS --------------------

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
      if (!_handlers.TryGetValue(command.GetType(), out var handler))
        throw new InvalidOperationException($"No handler registered for {command.GetType().Name}");

      // Pre-processors
      foreach (var pre in _preProcessors.OfType<IPreProcessor<ICommand<TResponse>>>())
        await pre.ProcessAsync(command, cancellationToken);

      // Pipeline chain
      Func<Task<TResponse>> handlerDelegate = () => ((dynamic)handler).Handle((dynamic)command, cancellationToken);

      foreach (var pipe in _pipelines.OfType<IPipeline<ICommand<TResponse>, TResponse>>().Reverse())
      {
        var next = handlerDelegate;
        handlerDelegate = () => pipe.Handle(command, next, cancellationToken);
      }

      var result = await handlerDelegate();

      // Post-processors
      foreach (var post in _postProcessors.OfType<IPostProcessor<ICommand<TResponse>, TResponse>>())
        await post.ProcessAsync(command, result, cancellationToken);

      return result;
    }

    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        => SendAsync<Unit>(new WrapperCommand(command), cancellationToken);

    private record WrapperCommand(ICommand Inner) : ICommand<Unit>;

    // -------------------- QUERIES --------------------

    public async Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
      if (!_handlers.TryGetValue(query.GetType(), out var handler))
        throw new InvalidOperationException($"No handler registered for {query.GetType().Name}");

      foreach (var pre in _preProcessors.OfType<IPreProcessor<IQuery<TResponse>>>())
        await pre.ProcessAsync(query, cancellationToken);

      Func<Task<TResponse>> handlerDelegate = () => ((dynamic)handler).Handle((dynamic)query, cancellationToken);

      foreach (var pipe in _pipelines.OfType<IPipeline<IQuery<TResponse>, TResponse>>().Reverse())
      {
        var next = handlerDelegate;
        handlerDelegate = () => pipe.Handle(query, next, cancellationToken);
      }

      var result = await handlerDelegate();

      foreach (var post in _postProcessors.OfType<IPostProcessor<IQuery<TResponse>, TResponse>>())
        await post.ProcessAsync(query, result, cancellationToken);

      return result;
    }

    // -------------------- NOTIFICATIONS --------------------

    public async Task PublishNotificationAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default,
        DispatchingStrategies.PublishStrategy strategy = DispatchingStrategies.PublishStrategy.Sequential,
        DispatchingStrategies.NotificationErrorHandling errorHandling = DispatchingStrategies.NotificationErrorHandling.StopOnFirstFailure)
        where TNotification : INotification
    {
      foreach (var handler in _notificationHandlers.OfType<INotificationHandler<TNotification>>())
      {
        Func<Task> handlerDelegate = () => handler.Handle(notification, cancellationToken);

        foreach (var pipe in _notificationPipelines.OfType<INotificationPipeline<TNotification>>().Reverse())
        {
          var next = handlerDelegate;
          handlerDelegate = () => pipe.Handle(notification, next, cancellationToken);
        }

        try
        {
          await handlerDelegate();
        }
        catch when (errorHandling == DispatchingStrategies.NotificationErrorHandling.ContinueOnError)
        {
          // swallow
        }
      }
    }

    // -------------------- STREAMS --------------------

    public async IAsyncEnumerable<TResponse> Stream<TQuery, TResponse>(TQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
       where TQuery : IStreamQuery<TResponse>
    {
      if (!_handlers.TryGetValue(query.GetType(), out var handler))
        throw new InvalidOperationException($"No stream handler registered for {query.GetType().Name}");

      var stream = (IAsyncEnumerable<TResponse>)((dynamic)handler).Handle((dynamic)query, cancellationToken);

      await foreach (var item in stream.WithCancellation(cancellationToken))
      {
        yield return item;
      }
    }

    public Task Send(INotification notification, CancellationToken cancellationToken = default)
    {
      throw new NotImplementedException();
    }

    public Task PublishEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
      throw new NotImplementedException();
    }
  }
}
