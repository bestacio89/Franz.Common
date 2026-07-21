using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Processors;
using Franz.Common.Mediator.Validation.Events;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Franz.Common.Mediator.Dispatchers.DispatchingStrategies;
using INotification = Franz.Common.Mediator.Messages.INotification;

namespace Franz.Common.Mediator.Dispatchers;

public class FranzDispatcher : IDispatcher
{
  private readonly IServiceProvider _serviceProvider;

  private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object?>>> ResponseHandlerCache = new();
  private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> VoidHandlerCache = new();
  private static readonly ConcurrentDictionary<Type, Func<FranzDispatcher, IEvent, CancellationToken, Task>> EventPublisherCache = new();

  private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> PreProcessorInvokerCache = new();
  private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Func<IServiceProvider, object, object?, CancellationToken, Task>> PostProcessorInvokerCache = new();

  public FranzDispatcher(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

  // ==================== COMMANDS ====================

  public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(command);

    MediatorContext.Reset();
    MediatorContext.EnsureCorrelationId();
    var context = MediatorContext.Current;
    var start = DateTime.UtcNow;

    await NotifyRequestStarted(command, context.CorrelationId, ct).ConfigureAwait(false);

    try
    {
      await RunPreProcessorsDynamic(command, ct).ConfigureAwait(false);

      var commandType = command.GetType();
      var invoker = ResponseHandlerCache.GetOrAdd(commandType, static t => CompileResponseInvoker(t, typeof(TResponse)));

      var pipelines = ResolveServices<IPipeline<ICommand<TResponse>, TResponse>>();

      var response = await PipelineExecutor.ExecuteAsync(
          command,
          pipelines,
          async (req, token) =>
          {
            var result = await invoker(_serviceProvider, req, token).ConfigureAwait(false);
            return (TResponse)result!;
          },
          ct).ConfigureAwait(false);

      await RunPostProcessorsDynamic(command, response, ct).ConfigureAwait(false);

      var duration = DateTime.UtcNow - start;
      await NotifyRequestCompleted(command, response, context.CorrelationId, duration, ct).ConfigureAwait(false);

      return response;
    }
    catch (Exception ex)
    {
      var duration = DateTime.UtcNow - start;
      await NotifyRequestFailed(command, ex, context.CorrelationId, duration, ct).ConfigureAwait(false);
      throw;
    }
  }

  public async Task SendAsync(ICommand command, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(command);

    MediatorContext.Reset();
    MediatorContext.EnsureCorrelationId();
    var context = MediatorContext.Current;
    var start = DateTime.UtcNow;

    await NotifyRequestStarted(command, context.CorrelationId, ct).ConfigureAwait(false);

    try
    {
      await RunPreProcessorsDynamic(command, ct).ConfigureAwait(false);

      var commandType = command.GetType();
      var invoker = VoidHandlerCache.GetOrAdd(commandType, static t => CompileVoidInvoker(t));

      var pipelines = ResolveServices<IPipeline<ICommand, Unit>>();

      await PipelineExecutor.ExecuteAsync(
          command,
          pipelines,
          async (req, token) =>
          {
            await invoker(_serviceProvider, req, token).ConfigureAwait(false);
            return Unit.Value;
          },
          ct).ConfigureAwait(false);

      await RunPostProcessorsDynamic(command, Unit.Value, ct).ConfigureAwait(false);

      var duration = DateTime.UtcNow - start;
      await NotifyRequestCompleted(command, Unit.Value, context.CorrelationId, duration, ct).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      var duration = DateTime.UtcNow - start;
      await NotifyRequestFailed(command, ex, context.CorrelationId, duration, ct).ConfigureAwait(false);
      throw;
    }
  }

  // ==================== QUERIES ====================

  public async Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(query);

    MediatorContext.Reset();
    MediatorContext.EnsureCorrelationId();
    var context = MediatorContext.Current;
    var start = DateTime.UtcNow;

    await NotifyRequestStarted(query, context.CorrelationId, ct).ConfigureAwait(false);

    try
    {
      await RunPreProcessorsDynamic(query, ct).ConfigureAwait(false);

      var queryType = query.GetType();
      var invoker = ResponseHandlerCache.GetOrAdd(queryType, static t => CompileQueryInvoker(t, typeof(TResponse)));

      var pipelines = ResolveServices<IPipeline<IQuery<TResponse>, TResponse>>();

      var response = await PipelineExecutor.ExecuteAsync(
          query,
          pipelines,
          async (req, token) =>
          {
            var result = await invoker(_serviceProvider, req, token).ConfigureAwait(false);
            return (TResponse)result!;
          },
          ct).ConfigureAwait(false);

      await RunPostProcessorsDynamic(query, response, ct).ConfigureAwait(false);

      var duration = DateTime.UtcNow - start;
      await NotifyRequestCompleted(query, response, context.CorrelationId, duration, ct).ConfigureAwait(false);

      return response;
    }
    catch (Exception ex)
    {
      var duration = DateTime.UtcNow - start;
      await NotifyRequestFailed(query, ex, context.CorrelationId, duration, ct).ConfigureAwait(false);
      throw;
    }
  }

  // ==================== NOTIFICATIONS ====================

  public async Task PublishNotificationAsync<TNotification>(
      TNotification notification,
      CancellationToken cancellationToken = default,
      PublishStrategy strategy = PublishStrategy.Sequential,
      NotificationErrorHandling errorHandling = NotificationErrorHandling.ContinueOnError)
      where TNotification : INotification
  {
    ArgumentNullException.ThrowIfNull(notification);

    if (errorHandling == NotificationErrorHandling.StopOnFirstFailure)
    {
      await PublishNotificationCore(notification, cancellationToken, strategy, errorHandling).ConfigureAwait(false);
      return;
    }

    try
    {
      await PublishNotificationCore(notification, cancellationToken, strategy, errorHandling).ConfigureAwait(false);
    }
    catch
    {
      // Best-effort policy: observe and swallow exceptions
    }
  }

  private async Task PublishNotificationCore<TNotification>(
      TNotification notification,
      CancellationToken cancellationToken,
      PublishStrategy strategy,
      NotificationErrorHandling errorHandling)
      where TNotification : INotification
  {
    var handlers = ResolveServices<INotificationHandler<TNotification>>();
    if (handlers.Count == 0)
      return;

    var pipelines = ResolveServices<INotificationPipeline<TNotification>>();
    var observers = ResolveServices<IMediatorObserver>();
    var correlationId = MediatorContext.Current.CorrelationId;

    async Task InvokeOneHandlerAsync(INotificationHandler<TNotification> handler)
    {
      var handlerType = handler.GetType();
      var start = DateTime.UtcNow;

      await NotifyNotificationHandlerStarted(notification, handlerType, correlationId, observers, cancellationToken).ConfigureAwait(false);

      try
      {
        Func<Task> handlerExecution = () => handler.Handle(notification, cancellationToken);

        for (int i = pipelines.Count - 1; i >= 0; i--)
        {
          var pipeline = pipelines[i];
          var current = handlerExecution;
          handlerExecution = () => pipeline.Handle(notification, current, cancellationToken);
        }

        await handlerExecution().ConfigureAwait(false);

        var duration = DateTime.UtcNow - start;
        await NotifyNotificationHandlerCompleted(notification, handlerType, correlationId, duration, observers, cancellationToken).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        var duration = DateTime.UtcNow - start;
        await NotifyNotificationHandlerFailed(notification, handlerType, correlationId, ex, duration, observers, cancellationToken).ConfigureAwait(false);

        if (errorHandling == NotificationErrorHandling.StopOnFirstFailure)
          throw;
      }
    }

    if (strategy == PublishStrategy.Sequential)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        await InvokeOneHandlerAsync(handlers[i]).ConfigureAwait(false);
      }
    }
    else if (strategy == PublishStrategy.Parallel)
    {
      var tasks = new Task[handlers.Count];
      for (int i = 0; i < handlers.Count; i++)
      {
        tasks[i] = InvokeOneHandlerAsync(handlers[i]);
      }
      await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    else
    {
      throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
    }
  }

  // ==================== EVENTS ====================

  public async Task PublishEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
      where TEvent : IEvent
  {
    ArgumentNullException.ThrowIfNull(@event);

    MediatorContext.Reset();
    MediatorContext.EnsureCorrelationId();
    var context = MediatorContext.Current;
    var start = DateTime.UtcNow;

    await NotifyRequestStarted(@event, context.CorrelationId, cancellationToken).ConfigureAwait(false);

    try
    {
      var handlers = ResolveServices<IEventHandler<TEvent>>();
      var pipelines = ResolveServices<IEventPipeline<TEvent>>();

      Func<Task> handlerChain = () =>
      {
        if (handlers.Count == 0) return Task.CompletedTask;
        var tasks = new Task[handlers.Count];
        for (int i = 0; i < handlers.Count; i++)
        {
          tasks[i] = handlers[i].HandleAsync(@event, cancellationToken);
        }
        return Task.WhenAll(tasks);
      };

      for (int i = pipelines.Count - 1; i >= 0; i--)
      {
        var pipeline = pipelines[i];
        var next = handlerChain;
        handlerChain = () => pipeline.HandleAsync(@event, next, cancellationToken);
      }

      await handlerChain().ConfigureAwait(false);

      var duration = DateTime.UtcNow - start;
      await NotifyRequestCompleted(@event, null, context.CorrelationId, duration, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      var duration = DateTime.UtcNow - start;
      await NotifyRequestFailed(@event, ex, context.CorrelationId, duration, cancellationToken).ConfigureAwait(false);
      throw;
    }
  }

  public Task PublishEventAsync(IEvent @event, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(@event);

    var eventType = @event.GetType();
    var publisher = EventPublisherCache.GetOrAdd(eventType, static t => CompileEventPublisher(t));

    return publisher(this, @event, ct);
  }

  // ==================== STREAMING ====================

  public async IAsyncEnumerable<TResponse> Stream<TQuery, TResponse>(
      TQuery query,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
      where TQuery : IStreamQuery<TResponse>
  {
    ArgumentNullException.ThrowIfNull(query);

    MediatorContext.Reset();
    MediatorContext.EnsureCorrelationId();
    var correlationId = MediatorContext.Current.CorrelationId;
    var start = DateTime.UtcNow;

    await NotifyRequestStarted(query, correlationId, cancellationToken).ConfigureAwait(false);

    Exception? capturedException = null;
    IAsyncEnumerator<TResponse>? enumerator = null;

    try
    {
      var handler = _serviceProvider.GetRequiredService<IStreamQueryHandler<TQuery, TResponse>>();
      var stream = handler.Handle(query, cancellationToken);
      enumerator = stream.GetAsyncEnumerator(cancellationToken);
    }
    catch (Exception ex)
    {
      capturedException = ex;
      var duration = DateTime.UtcNow - start;
      await NotifyRequestFailed(query, capturedException, correlationId, duration, cancellationToken).ConfigureAwait(false);
      throw;
    }

    try
    {
      while (true)
      {
        bool hasNext;
        try
        {
          hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          capturedException = ex;
          throw;
        }

        if (!hasNext) break;

        yield return enumerator.Current;
      }
    }
    finally
    {
      if (enumerator is not null)
        await enumerator.DisposeAsync().ConfigureAwait(false);

      var duration = DateTime.UtcNow - start;

      if (capturedException is null)
        await NotifyRequestCompleted(query, null, correlationId, duration, cancellationToken).ConfigureAwait(false);
      else if (enumerator is not null)
        await NotifyRequestFailed(query, capturedException, correlationId, duration, cancellationToken).ConfigureAwait(false);
    }
  }

  // ==================== HELPERS & OBSERVERS ====================

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private IReadOnlyList<TService> ResolveServices<TService>()
  {
    return _serviceProvider.GetServices<TService>() as IReadOnlyList<TService>
           ?? _serviceProvider.GetServices<TService>().ToArray();
  }

  private Task RunPreProcessorsDynamic(object request, CancellationToken ct)
  {
    var requestType = request.GetType();
    var invoker = PreProcessorInvokerCache.GetOrAdd(requestType, CompilePreProcessorInvoker);
    return invoker(_serviceProvider, request, ct);
  }

  private Task RunPostProcessorsDynamic<TResponse>(object request, TResponse response, CancellationToken ct)
  {
    var requestType = request.GetType();
    var key = (RequestType: requestType, ResponseType: typeof(TResponse));
    var invoker = PostProcessorInvokerCache.GetOrAdd(key, k => CompilePostProcessorInvoker(k.RequestType, k.ResponseType));
    return invoker(_serviceProvider, request, response, ct);
  }

  private static Func<IServiceProvider, object, CancellationToken, Task> CompilePreProcessorInvoker(Type requestType)
  {
    return (sp, req, ct) =>
    {
      var processorType = typeof(IPreProcessor<>).MakeGenericType(requestType);
      var processors = sp.GetServices(processorType);

      var method = processorType.GetMethod(nameof(IPreProcessor<object>.ProcessAsync))!;

      async Task ExecuteAll()
      {
        foreach (var p in processors)
        {
          if (p is not null)
          {
            var task = (Task)method.Invoke(p, new[] { req, ct })!;
            await task.ConfigureAwait(false);
          }
        }
      }

      return ExecuteAll();
    };
  }

  private static Func<IServiceProvider, object, object?, CancellationToken, Task> CompilePostProcessorInvoker(Type requestType, Type responseType)
  {
    return (sp, req, res, ct) =>
    {
      var processorType = typeof(IPostProcessor<,>).MakeGenericType(requestType, responseType);
      var processors = sp.GetServices(processorType);

      var method = processorType.GetMethod(nameof(IPostProcessor<object, object>.ProcessAsync))!;

      async Task ExecuteAll()
      {
        foreach (var p in processors)
        {
          if (p is not null)
          {
            var task = (Task)method.Invoke(p, new[] { req, res, ct })!;
            await task.ConfigureAwait(false);
          }
        }
      }

      return ExecuteAll();
    };
  }

  private async Task NotifyRequestStarted(object request, Guid correlationId, CancellationToken ct)
  {
    var observers = ResolveServices<IMediatorObserver>();
    for (int i = 0; i < observers.Count; i++)
      await observers[i].OnRequestStarted(request, correlationId, ct).ConfigureAwait(false);
  }

  private async Task NotifyRequestCompleted(object request, object? response, Guid correlationId, TimeSpan duration, CancellationToken ct)
  {
    var observers = ResolveServices<IMediatorObserver>();
    for (int i = 0; i < observers.Count; i++)
      await observers[i].OnRequestCompleted(request, response, correlationId, duration, ct).ConfigureAwait(false);
  }

  private async Task NotifyRequestFailed(object request, Exception exception, Guid correlationId, TimeSpan duration, CancellationToken ct)
  {
    var observers = ResolveServices<IMediatorObserver>();
    for (int i = 0; i < observers.Count; i++)
      await observers[i].OnRequestFailed(request, exception, correlationId, duration, ct).ConfigureAwait(false);
  }

  private static async Task NotifyNotificationHandlerStarted(object notification, Type handlerType, Guid correlationId, IReadOnlyList<IMediatorObserver> observers, CancellationToken ct)
  {
    for (int i = 0; i < observers.Count; i++)
      await observers[i].OnNotificationHandlerStarted(notification, handlerType, correlationId, ct).ConfigureAwait(false);
  }

  private static async Task NotifyNotificationHandlerCompleted(object notification, Type handlerType, Guid correlationId, TimeSpan duration, IReadOnlyList<IMediatorObserver> observers, CancellationToken ct)
  {
    for (int i = 0; i < observers.Count; i++)
      await observers[i].OnNotificationHandlerCompleted(notification, handlerType, correlationId, duration, ct).ConfigureAwait(false);
  }

  private static async Task NotifyNotificationHandlerFailed(object notification, Type handlerType, Guid correlationId, Exception ex, TimeSpan duration, IReadOnlyList<IMediatorObserver> observers, CancellationToken ct)
  {
    for (int i = 0; i < observers.Count; i++)
      await observers[i].OnNotificationHandlerFailed(notification, handlerType, correlationId, ex, duration, ct).ConfigureAwait(false);
  }

  // ==================== EXPRESSION TREE COMPILERS ====================

  private static Func<IServiceProvider, object, CancellationToken, Task<object?>> CompileResponseInvoker(Type requestType, Type responseType)
  {
    var handlerType = typeof(ICommandHandler<,>).MakeGenericType(requestType, responseType);
    return BuildCompiledResponseInvoker(handlerType, requestType, responseType);
  }

  private static Func<IServiceProvider, object, CancellationToken, Task<object?>> CompileQueryInvoker(Type queryType, Type responseType)
  {
    var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, responseType);
    return BuildCompiledResponseInvoker(handlerType, queryType, responseType);
  }

  private static Func<IServiceProvider, object, CancellationToken, Task<object?>> BuildCompiledResponseInvoker(Type handlerType, Type requestType, Type responseType)
  {
    var providerParam = Expression.Parameter(typeof(IServiceProvider), "sp");
    var requestParam = Expression.Parameter(typeof(object), "req");
    var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

    var getServiceMethod = typeof(ServiceProviderServiceExtensions)
        .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), new[] { typeof(IServiceProvider), typeof(Type) })!;

    var handlerInstance = Expression.Call(null, getServiceMethod, providerParam, Expression.Constant(handlerType));
    var castHandler = Expression.Convert(handlerInstance, handlerType);
    var castRequest = Expression.Convert(requestParam, requestType);

    var handleMethod = handlerType.GetMethod("Handle", new[] { requestType, typeof(CancellationToken) })!;
    var callHandle = Expression.Call(castHandler, handleMethod, castRequest, ctParam);

    var helperMethod = typeof(FranzDispatcher).GetMethod(nameof(CastTaskToObject), BindingFlags.NonPublic | BindingFlags.Static)!
        .MakeGenericMethod(responseType);

    var body = Expression.Call(null, helperMethod, callHandle);

    return Expression.Lambda<Func<IServiceProvider, object, CancellationToken, Task<object?>>>(body, providerParam, requestParam, ctParam).Compile();
  }

  private static Func<IServiceProvider, object, CancellationToken, Task> CompileVoidInvoker(Type commandType)
  {
    var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);

    var providerParam = Expression.Parameter(typeof(IServiceProvider), "sp");
    var commandParam = Expression.Parameter(typeof(object), "cmd");
    var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

    var getServiceMethod = typeof(ServiceProviderServiceExtensions)
        .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), new[] { typeof(IServiceProvider), typeof(Type) })!;

    var handlerInstance = Expression.Call(null, getServiceMethod, providerParam, Expression.Constant(handlerType));
    var castHandler = Expression.Convert(handlerInstance, handlerType);
    var castCommand = Expression.Convert(commandParam, commandType);

    var handleMethod = handlerType.GetMethod("Handle", new[] { commandType, typeof(CancellationToken) })!;
    var body = Expression.Call(castHandler, handleMethod, castCommand, ctParam);

    return Expression.Lambda<Func<IServiceProvider, object, CancellationToken, Task>>(body, providerParam, commandParam, ctParam).Compile();
  }

  private static Func<FranzDispatcher, IEvent, CancellationToken, Task> CompileEventPublisher(Type eventType)
  {
    var dispatcherParam = Expression.Parameter(typeof(FranzDispatcher), "dispatcher");
    var eventParam = Expression.Parameter(typeof(IEvent), "evt");
    var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

    var castEvent = Expression.Convert(eventParam, eventType);
    var method = typeof(FranzDispatcher).GetMethods()
        .First(m => m.Name == nameof(PublishEventAsync) && m.IsGenericMethod)
        .MakeGenericMethod(eventType);

    var body = Expression.Call(dispatcherParam, method, castEvent, ctParam);

    return Expression.Lambda<Func<FranzDispatcher, IEvent, CancellationToken, Task>>(body, dispatcherParam, eventParam, ctParam).Compile();
  }

  private static async Task<object?> CastTaskToObject<T>(Task<T> task)
  {
    return await task.ConfigureAwait(false);
  }
}