using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Processors;
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

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
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

    public Task Send(ICommand command, CancellationToken cancellationToken = default)
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

    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
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
        NotificationErrorHandling errorHandling = NotificationErrorHandling.StopOnFirstFailure)
        where TNotification : INotification
    {
      return ExecuteWithObservability(notification, async () =>
      {
        var handlers = _serviceProvider.GetService<IEnumerable<INotificationHandler<TNotification>>>()
                      ?? Array.Empty<INotificationHandler<TNotification>>();

        var pipelines = _serviceProvider.GetServices<INotificationPipeline<TNotification>>().ToList();
        var observers = _serviceProvider.GetServices<IMediatorObserver>().ToList();

        // Build the per-handler pipeline chain
        Func<INotificationHandler<TNotification>, Task> buildHandlerChain =
          handler =>
          {
            Func<Task> handlerDelegate = () => handler.Handle(notification, cancellationToken);

            foreach (var pipeline in pipelines.AsEnumerable().Reverse())
            {
              var next = handlerDelegate;
              handlerDelegate = () => pipeline.Handle(notification, next, cancellationToken);
            }

            return handlerDelegate();
          };

        var correlationId = MediatorContext.Current.CorrelationId;

        switch (strategy)
        {
          case PublishStrategy.Sequential:
            foreach (var handler in handlers)
            {
              var handlerType = handler.GetType();
              var start = DateTime.UtcNow;

              await NotifyNotificationHandlerStarted(notification!, handlerType, correlationId, observers, cancellationToken);

              try
              {
                await buildHandlerChain(handler);

                var duration = DateTime.UtcNow - start;
                await NotifyNotificationHandlerCompleted(notification!, handlerType, correlationId, duration, observers, cancellationToken);
              }
              catch (Exception ex)
              {
                var duration = DateTime.UtcNow - start;
                await NotifyNotificationHandlerFailed(notification!, handlerType, correlationId, ex, duration, observers, cancellationToken);

                if (errorHandling == NotificationErrorHandling.StopOnFirstFailure)
                  throw;
                // else ContinueOnError: swallow and move to next handler
              }
            }
            break;

          case PublishStrategy.Parallel:
            var tasks = handlers.Select(async handler =>
            {
              var handlerType = handler.GetType();
              var start = DateTime.UtcNow;

              await NotifyNotificationHandlerStarted(notification!, handlerType, correlationId, observers, cancellationToken);

              try
              {
                await buildHandlerChain(handler);

                var duration = DateTime.UtcNow - start;
                await NotifyNotificationHandlerCompleted(notification!, handlerType, correlationId, duration, observers, cancellationToken);
              }
              catch (Exception ex)
              {
                var duration = DateTime.UtcNow - start;
                await NotifyNotificationHandlerFailed(notification!, handlerType, correlationId, ex, duration, observers, cancellationToken);

                if (errorHandling == NotificationErrorHandling.StopOnFirstFailure)
                  throw; // let Task.WhenAll surface the aggregate
                // ContinueOnError: swallow
              }
            });

            await Task.WhenAll(tasks);
            break;
        }

        return (object?)null; // no response for Publish
      }, cancellationToken);
    }

    // -------------------- STREAMING --------------------

    public async IAsyncEnumerable<TResponse> Stream<TQuery, TResponse>(
      TQuery query,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
      where TQuery : IStreamQuery<TResponse>
    {
      // FIX: use static Reset
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

        while (true)
        {
          bool hasNext;
          try
          {
            hasNext = await enumerator.MoveNextAsync();
          }
          catch (Exception ex)
          {
            capturedException = ex;
            throw;
          }

          if (!hasNext)
            break;

          yield return enumerator.Current;
        }
      }
      finally
      {
        if (enumerator is not null)
          await enumerator.DisposeAsync();

        var duration = DateTime.UtcNow - start;

        if (capturedException is null)
        {
          await NotifyRequestCompleted(query, null, correlationId, duration, cancellationToken);
        }
        else
        {
          await NotifyRequestFailed(query, capturedException, correlationId, duration, cancellationToken);
        }
      }
    }

    // -------------------- OBSERVER NOTIFICATIONS (REQUESTS) --------------------

    private async Task NotifyRequestStarted(object request, string correlationId, CancellationToken cancellationToken)
    {
      foreach (var observer in _serviceProvider.GetServices<IMediatorObserver>())
      {
        await observer.OnRequestStarted(request, correlationId, cancellationToken);
      }
    }

    private async Task NotifyRequestCompleted(object request, object? response, string correlationId, TimeSpan duration, CancellationToken cancellationToken)
    {
      foreach (var observer in _serviceProvider.GetServices<IMediatorObserver>())
      {
        await observer.OnRequestCompleted(request, response, correlationId, duration, cancellationToken);
      }
    }

    private async Task NotifyRequestFailed(object request, Exception exception, string correlationId, TimeSpan duration, CancellationToken cancellationToken)
    {
      foreach (var observer in _serviceProvider.GetServices<IMediatorObserver>())
      {
        await observer.OnRequestFailed(request, exception, correlationId, duration, cancellationToken);
      }
    }

    // -------------------- OBSERVER NOTIFICATIONS (PER-HANDLER) --------------------

    private static async Task NotifyNotificationHandlerStarted(
      object notification,
      Type handlerType,
      string correlationId,
      IList<IMediatorObserver> observers,
      CancellationToken cancellationToken)
    {
      foreach (var o in observers)
      {
        await o.OnNotificationHandlerStarted(notification, handlerType, correlationId, cancellationToken);
      }
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
      {
        await o.OnNotificationHandlerCompleted(notification, handlerType, correlationId, duration, cancellationToken);
      }
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
      {
        await o.OnNotificationHandlerFailed(notification, handlerType, correlationId, exception, duration, cancellationToken);
      }
    }

    public Task Send(INotification notification, CancellationToken cancellationToken = default)
    {
      // Delegate directly to PublishAsync<TNotification> 
      // Default: sequential execution, stop on first failure
      var method = typeof(FranzDispatcher)
          .GetMethod(nameof(PublishAsync))
          !.MakeGenericMethod(notification.GetType());

      return (Task)method.Invoke(this, new object[]
      {
        notification,
        cancellationToken,
        PublishStrategy.Sequential,
        NotificationErrorHandling.StopOnFirstFailure
      })!;
    }

    // -------------------- EXECUTION WRAPPER --------------------

    private async Task<TResult> ExecuteWithObservability<TResult>(
        object request,
        Func<Task<TResult>> execute,
        CancellationToken cancellationToken)
    {
      // FIX: use static Reset
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
