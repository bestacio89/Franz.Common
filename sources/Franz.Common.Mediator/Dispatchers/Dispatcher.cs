using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines;
using MediatR; // optional, for MediatR-compatible pipeline behaviors
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Dispatchers
{
  public class Dispatcher : IDispatcher
  {
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
    }

    // -------------------- COMMANDS --------------------

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
      var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
      dynamic handler = _serviceProvider.GetRequiredService(handlerType);

      Func<Task<TResponse>> handlerDelegate = () => handler.Handle((dynamic)command, cancellationToken);

      handlerDelegate = BuildPipelineChain(command, handlerDelegate, cancellationToken);

      return handlerDelegate();
    }

    public Task Send(ICommand command, CancellationToken cancellationToken = default)
    {
      var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(Unit));
      dynamic handler = _serviceProvider.GetRequiredService(handlerType);

      Func<Task<Unit>> handlerDelegate = () => handler.Handle((dynamic)command, cancellationToken);

      var pipelineDelegate = BuildPipelineChain((dynamic)command, handlerDelegate, cancellationToken);

      return pipelineDelegate();
    }

    // -------------------- QUERIES --------------------

    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
      var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
      dynamic handler = _serviceProvider.GetRequiredService(handlerType);

      Func<Task<TResponse>> handlerDelegate = () => handler.Handle((dynamic)query, cancellationToken);

      handlerDelegate = BuildPipelineChain(query, handlerDelegate, cancellationToken);

      return handlerDelegate();
    }

    // -------------------- PIPELINE BUILDER --------------------

    private Func<Task<TResponse>> BuildPipelineChain<TRequest, TResponse> (
        TRequest request,
        Func<Task<TResponse>> finalHandler,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
      // Franz-native pipelines
      var franzPipelines = _serviceProvider.GetServices<IPipeline<TRequest, TResponse>>().ToList();

      // MediatR-compatible pipelines (optional)
      var mediatrPipelines = _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToList();

      Func<Task<TResponse>> pipelineDelegate = finalHandler;

      // Apply Franz-native pipelines in reverse order
      foreach (var pipeline in franzPipelines.AsEnumerable().Reverse())
      {
        var next = pipelineDelegate;
        pipelineDelegate = () => pipeline.Handle(request, next, cancellationToken);
      }

      // Apply MediatR pipelines in reverse order
      foreach (var pipeline in mediatrPipelines.AsEnumerable().Reverse())
      {
        var next = pipelineDelegate;

        // MediatR expects RequestHandlerDelegate, adapt delegate signature
        Task<TResponse> RequestHandlerDelegate(CancellationToken ct) => next();

        pipelineDelegate = () => pipeline.Handle(request, RequestHandlerDelegate, cancellationToken);
      }

      return pipelineDelegate;
    }
  }
}
