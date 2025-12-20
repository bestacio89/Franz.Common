using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Processors;
using Franz.Common.Mediator.Validation.Events;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Franz.Common.Mediator.Dispatchers.DispatchingStrategies;
using INotification = Franz.Common.Mediator.Messages.INotification;

namespace Franz.Common.Mediator.Dispatchers
{
  public class FranzDispatcher : IDispatcher
  {
    private readonly IServiceProvider _serviceProvider;

    public FranzDispatcher(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
    }

    // -------------------- COMMANDS --------------------

    public Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
      return ExecuteWithObservability(command, async () =>
      {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        dynamic handler = _serviceProvider.GetRequiredService(handlerType);

        await RunPreProcessors(command, cancellationToken);

        Func<Task<TResponse>> handlerDelegate = () => handler.Handle((dynamic)command, cancellationToken);
        handlerDelegate = BuildPipelineChain(command, handlerDelegate, cancellationToken);

        var response = await handlerDelegate();

        await RunPostProcessors(command, response, cancellationToken);

        return response;
      }, cancellationToken);
    }

    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
      return ExecuteWithObservability(command, async () =>
      {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(Unit));
        dynamic handler = _serviceProvider.GetRequiredService(handlerType);

        await RunPreProcessors(command, cancellationToken);

        Func<Task<Unit>> handlerDelegate = () => handler.Handle((dynamic)command, cancellationToken);
        var pipelineDelegate = BuildPipelineChain((dynamic)command, handlerDelegate, cancellationToken);

        var response = await pipelineDelegate();

        await RunPostProcessors(command, response, cancellationToken);

        return (object?)null; // Send() has no response
      }, cancellationToken);
    }

    // -------------------- QUERIES --------------------

    public Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
      return ExecuteWithObservability(query, async () =>
      {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        dynamic handler = _serviceProvider.GetRequiredService(handlerType);

        await RunPreProcessors(query, cancellationToken);

        Func<Task<TResponse>> handlerDelegate = () => handler.Handle((dynamic)query, cancellationToken);
        handlerDelegate = BuildPipelineChain(query, handlerDelegate, cancellationToken);

        var response = await handlerDelegate();

        await RunPostProcessors(query, response, cancellationToken);

        return response;
      }, cancellationToken);
    }

    // -------------------- PIPELINE BUILDER --------------------

    private Func<Task<TResponse>> BuildPipelineChain<TRequest, TResponse>(
        TRequest request,
        Func<Task<TResponse>> finalHandler,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
      var franzPipelines = _serviceProvider.GetServices<IPipeline<TRequest, TResponse>>().ToList();

      Func<Task<TResponse>> pipelineDelegate = finalHandler;

      foreach (var pipeline in franzPipelines.AsEnumerable().Reverse())
      {
        var next = pipelineDelegate;
        pipelineDelegate = () => pipeline.Handle(request, next, cancellationToken);
      }

      return pipelineDelegate;
    }

    // -------------------- PRE/POST PROCESSORS --------------------

    private async Task RunPreProcessors<TRequest>(TRequest request, CancellationToken cancellationToken)
    {
      foreach (var processor in _serviceProvider.GetServices<IPreProcessor<TRequest>>())
      {
        await processor.ProcessAsync(request, cancellationToken);
      }
    }

    private async Task RunPostProcessors<TRequest, TResponse>(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
      foreach (var processor in _serviceProvider.GetServices<IPostProcessor<TRequest, TResponse>>())
      {
        await processor.ProcessAsync(request, response, cancellationToken);
      }
    }

    // -------------------- NOTIFICATIONS --------------------

    public Task PublishAsync<TNotification>(
       TNotification notification,
       CancellationToken cancellationToken = default,
       PublishStrategy strategy = PublishStrategy.Sequential,
       NotificationErrorHandling errorHandling = NotificationErrorHandling.ContinueOnError)
       where TNotification : INotification
    {
      return ExecuteWithObservability(notification, async () =>
      {
        try
        {
          var handlers = _serviceProvider
            .GetService<IEnumerable<INotificationHandler<TNotification>>>()
            ?? Enumerable.Empty<INotificationHandler<TNotification>>();

          var handlerList = handlers as IList<INotificationHandler<TNotification>>
                            ?? handlers.ToList();

          if (handlerList.Count == 0)
            return (object?)null;

          var pipelines = _serviceProvider
            .GetServices<INotificationPipeline<TNotification>>()
            .ToList();

          var observers = _serviceProvider
            .GetServices<IMediatorObserver>()
            .ToList();

          Func<INotificationHandler<TNotification>, Task> buildHandlerChain =
            handler =>
            {
              Func<Task> next = () => handler.Handle(notification, cancellationToken);

              foreach (var pipeline in pipelines.AsEnumerable().Reverse())
              {
                var current = next;
                next = () => pipeline.Handle(notification, current, cancellationToken);
              }

              return next();
            };

          var correlationId = MediatorContext.Current.CorrelationId;

          async Task InvokeOneHandlerAsync(INotificationHandler<TNotification> handler)
          {
            var handlerType = handler.GetType();
            var start = DateTime.UtcNow;

            await NotifyNotificationHandlerStarted(
              notification!,
              handlerType,
              correlationId,
              observers,
              cancellationToken);

            try
            {
              await buildHandlerChain(handler);

              var duration = DateTime.UtcNow - start;

              await NotifyNotificationHandlerCompleted(
                notification!,
                handlerType,
                correlationId,
                duration,
                observers,
                cancellationToken);
            }
            catch (Exception ex)
            {
              var duration = DateTime.UtcNow - start;

              await NotifyNotificationHandlerFailed(
                notification!,
                handlerType,
                correlationId,
                ex,
                duration,
                observers,
                cancellationToken);

              // 🔴 STRICT MODE ONLY
              if (errorHandling == NotificationErrorHandling.StopOnFirstFailure)
                throw;
            }
          }

          switch (strategy)
          {
            case PublishStrategy.Sequential:
              foreach (var handler in handlerList)
              {
                await InvokeOneHandlerAsync(handler);
              }
              break;

            case PublishStrategy.Parallel:
              var tasks = handlerList
                .Select(handler =>
                  Task.Run(() => InvokeOneHandlerAsync(handler), cancellationToken))
                .ToArray();

              if (errorHandling == NotificationErrorHandling.StopOnFirstFailure)
              {
                // Fail-fast: any handler failure faults the publish
                await Task.WhenAll(tasks);
              }
              else
              {
                // Best-effort: NEVER let Task.WhenAll fault the dispatcher
                try
                {
                  await Task.WhenAll(tasks);
                }
                catch
                {
                  // intentionally swallowed — failures already observed
                }
              }
              break;

            default:
              throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
          }

          return (object?)null;
        }
        catch when (errorHandling == NotificationErrorHandling.ContinueOnError)
        {
          // 🛡️ ABSOLUTE SAFETY NET
          // No notification must EVER fault the outer dispatcher task
          return (object?)null;
        }
      }, cancellationToken);
    }


    // -------------------- EVENTS --------------------

    // Generic version
    public Task PublishEventAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
      return ExecuteWithObservability(@event, async () =>
      {
        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>().ToList();
        var pipelines = _serviceProvider.GetServices<IEventPipeline<TEvent>>().ToList();

        Func<Task> handlerChain = () => Task.WhenAll(
            handlers.Select(h => h.HandleAsync(@event, cancellationToken)));

        foreach (var pipeline in pipelines.AsEnumerable().Reverse())
        {
          var next = handlerChain;
          handlerChain = () => pipeline.HandleAsync(@event, next, cancellationToken);
        }

        await handlerChain();
        return (object?)null;
      }, cancellationToken);
    }

    // Non-generic bridge version
    public Task PublishEventAsync(IEvent @event, CancellationToken ct = default)
    {
      var concreteType = @event.GetType(); // e.g. OrderCancelledEvent
      var method = typeof(FranzDispatcher)
          .GetMethod(nameof(PublishEventAsync), new[] { typeof(IEvent), typeof(CancellationToken) });

      // find the generic PublishEventAsync<TEvent>
      var generic = typeof(FranzDispatcher)
          .GetMethods()
          .First(m => m.Name == nameof(PublishEventAsync) && m.IsGenericMethod);

      var constructed = generic.MakeGenericMethod(concreteType);
      return (Task)constructed.Invoke(this, new object?[] { @event, ct })!;
    }


    // -------------------- STREAMING --------------------
    public async IAsyncEnumerable<TResponse> Stream<TQuery, TResponse>(
      TQuery query,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
      where TQuery : IStreamQuery<TResponse>
    {
      MediatorContext.Reset();

      var correlationId = MediatorContext.Current.CorrelationId;
      var start = DateTime.UtcNow;
      await NotifyRequestStarted(query, correlationId, cancellationToken);

      Exception? capturedException = null;
      IAsyncEnumerator<TResponse>? enumerator = null;

      try
      {
        var handler = _serviceProvider.GetRequiredService<IStreamQueryHandler<TQuery, TResponse>>();
        var stream = handler.Handle(query, cancellationToken);
        enumerator = stream.GetAsyncEnumerator(cancellationToken);

        while (await enumerator.MoveNextAsync())
        {
          yield return enumerator.Current;
        }
      }
      finally
      {
        if (enumerator is not null)
          await enumerator.DisposeAsync();

        var duration = DateTime.UtcNow - start;

        if (capturedException is null)
          await NotifyRequestCompleted(query, null, correlationId, duration, cancellationToken);
        else
          await NotifyRequestFailed(query, capturedException, correlationId, duration, cancellationToken);
      }
    }

    // -------------------- OBSERVER NOTIFICATIONS --------------------

    private async Task NotifyRequestStarted(object request, string correlationId, CancellationToken cancellationToken)
    {
      foreach (var observer in _serviceProvider.GetServices<IMediatorObserver>())
        await observer.OnRequestStarted(request, correlationId, cancellationToken);
    }

    private async Task NotifyRequestCompleted(object request, object? response, string correlationId, TimeSpan duration, CancellationToken cancellationToken)
    {
      foreach (var observer in _serviceProvider.GetServices<IMediatorObserver>())
        await observer.OnRequestCompleted(request, response, correlationId, duration, cancellationToken);
    }

    private async Task NotifyRequestFailed(object request, Exception exception, string correlationId, TimeSpan duration, CancellationToken cancellationToken)
    {
      foreach (var observer in _serviceProvider.GetServices<IMediatorObserver>())
        await observer.OnRequestFailed(request, exception, correlationId, duration, cancellationToken);
    }

    private static async Task NotifyNotificationHandlerStarted(
      object notification,
      Type handlerType,
      string correlationId,
      IList<IMediatorObserver> observers,
      CancellationToken cancellationToken)
    {
      foreach (var o in observers)
        await o.OnNotificationHandlerStarted(notification, handlerType, correlationId, cancellationToken);
    }

    private static async Task NotifyNotificationHandlerCompleted(
      object notification,
      Type handlerType,
      string correlationId,
      TimeSpan duration,
      IList<IMediatorObserver> observers,
      CancellationToken cancellationToken)
    {
      foreach (var o in observers)
        await o.OnNotificationHandlerCompleted(notification, handlerType, correlationId, duration, cancellationToken);
    }

    private static async Task NotifyNotificationHandlerFailed(
      object notification,
      Type handlerType,
      string correlationId,
      Exception exception,
      TimeSpan duration,
      IList<IMediatorObserver> observers,
      CancellationToken cancellationToken)
    {
      foreach (var o in observers)
        await o.OnNotificationHandlerFailed(notification, handlerType, correlationId, exception, duration, cancellationToken);
    }

    // -------------------- EXECUTION WRAPPER --------------------

    private async Task<TResult> ExecuteWithObservability<TResult>(
        object request,
        Func<Task<TResult>> execute,
        CancellationToken cancellationToken)
    {
      MediatorContext.Reset();

      var correlationId = MediatorContext.Current.CorrelationId;
      var start = DateTime.UtcNow;
      await NotifyRequestStarted(request, correlationId, cancellationToken);

      try
      {
        var result = await execute();
        var duration = DateTime.UtcNow - start;

        await NotifyRequestCompleted(request, result, correlationId, duration, cancellationToken);
        return result;
      }
      catch (Exception ex)
      {
        var duration = DateTime.UtcNow - start;
        await NotifyRequestFailed(request, ex, correlationId, duration, cancellationToken);
        throw;
      }
    }
  }
}
