using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Processors;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Registration;

/// <summary>
/// Holds strongly typed dispatch delegates emitted by the mediator source generator.
/// Runtime dispatch performs dictionary lookups only; it does not construct generic
/// types, invoke methods through reflection, or compile expressions.
/// </summary>
public static class GeneratedMediatorInvokerRegistry
{
  private static readonly ConcurrentDictionary<(Type Request, Type Response), Func<IServiceProvider, object, CancellationToken, Task<object?>>> CommandInvokers = new();
  private static readonly ConcurrentDictionary<(Type Request, Type Response), Func<IServiceProvider, object, CancellationToken, Task<object?>>> QueryInvokers = new();
  private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> VoidCommandInvokers = new();
  private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> PreProcessorInvokers = new();
  private static readonly ConcurrentDictionary<(Type Request, Type Response), Func<IServiceProvider, object, object?, CancellationToken, Task>> PostProcessorInvokers = new();
  private static readonly ConcurrentDictionary<Type, Func<FranzDispatcher, IEvent, CancellationToken, Task>> EventPublishers = new();

  public static void RegisterCommand<TCommand, TResponse>()
      where TCommand : ICommand<TResponse>
  {
    CommandInvokers.TryAdd(
        (typeof(TCommand), typeof(TResponse)),
        static async (services, request, cancellationToken) =>
            await services.GetRequiredService<ICommandHandler<TCommand, TResponse>>()
                .Handle((TCommand)request, cancellationToken).ConfigureAwait(false));
    RegisterProcessors<TCommand, TResponse>();
  }

  public static void RegisterQuery<TQuery, TResponse>()
      where TQuery : IQuery<TResponse>
  {
    QueryInvokers.TryAdd(
        (typeof(TQuery), typeof(TResponse)),
        static async (services, request, cancellationToken) =>
            await services.GetRequiredService<IQueryHandler<TQuery, TResponse>>()
                .Handle((TQuery)request, cancellationToken).ConfigureAwait(false));
    RegisterProcessors<TQuery, TResponse>();
  }

  public static void RegisterVoidCommand<TCommand>()
      where TCommand : ICommand
  {
    VoidCommandInvokers.TryAdd(
        typeof(TCommand),
        static (services, request, cancellationToken) =>
            services.GetRequiredService<ICommandHandler<TCommand>>()
                .Handle((TCommand)request, cancellationToken));
    RegisterPreProcessor<TCommand>();
  }

  public static void RegisterEvent<TEvent>()
      where TEvent : IEvent
  {
    EventPublishers.TryAdd(
        typeof(TEvent),
        static (dispatcher, @event, cancellationToken) =>
            dispatcher.PublishEventAsync((TEvent)@event, cancellationToken));
  }

  public static Func<IServiceProvider, object, CancellationToken, Task<object?>> GetCommandInvoker(Type requestType, Type responseType)
      => GetRequired(CommandInvokers, (requestType, responseType), requestType, "command");

  public static Func<IServiceProvider, object, CancellationToken, Task<object?>> GetQueryInvoker(Type requestType, Type responseType)
      => GetRequired(QueryInvokers, (requestType, responseType), requestType, "query");

  public static Func<IServiceProvider, object, CancellationToken, Task> GetVoidCommandInvoker(Type requestType)
      => GetRequired(VoidCommandInvokers, requestType, requestType, "void command");

  public static Func<IServiceProvider, object, CancellationToken, Task> GetPreProcessorInvoker(Type requestType)
      => GetRequired(PreProcessorInvokers, requestType, requestType, "pre-processor");

  public static Func<IServiceProvider, object, object?, CancellationToken, Task> GetPostProcessorInvoker(Type requestType, Type responseType)
      => GetRequired(PostProcessorInvokers, (requestType, responseType), requestType, "post-processor");

  public static Func<FranzDispatcher, IEvent, CancellationToken, Task> GetEventPublisher(Type eventType)
      => GetRequired(EventPublishers, eventType, eventType, "event publisher");

  private static void RegisterProcessors<TRequest, TResponse>()
  {
    RegisterPreProcessor<TRequest>();
    PostProcessorInvokers.TryAdd(
        (typeof(TRequest), typeof(TResponse)),
        static async (services, request, response, cancellationToken) =>
        {
          var processors = services.GetServices<IPostProcessor<TRequest, TResponse>>();
          foreach (var processor in processors)
          {
            await processor.ProcessAsync((TRequest)request, (TResponse)response!, cancellationToken)
                .ConfigureAwait(false);
          }
        });
  }

  private static void RegisterPreProcessor<TRequest>()
  {
    PreProcessorInvokers.TryAdd(
        typeof(TRequest),
        static async (services, request, cancellationToken) =>
        {
          var processors = services.GetServices<IPreProcessor<TRequest>>();
          foreach (var processor in processors)
          {
            await processor.ProcessAsync((TRequest)request, cancellationToken).ConfigureAwait(false);
          }
        });
  }

  private static TValue GetRequired<TKey, TValue>(
      ConcurrentDictionary<TKey, TValue> registrations,
      TKey key,
      Type requestType,
      string registrationKind)
      where TKey : notnull
  {
    if (registrations.TryGetValue(key, out var invoker))
      return invoker;

    throw new InvalidOperationException(
        $"No generated mediator {registrationKind} exists for '{requestType.FullName}'. " +
        "Ensure the request and its handler are compiled in a project using the Franz Mediator source generator.");
  }
}
