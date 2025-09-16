using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using INotification = Franz.Common.Mediator.Messages.INotification;


namespace Franz.Common.Mediator.Dispatchers
{
  public class MediatRDispatcherDecorator : IDispatcher
  {
    private readonly FranzDispatcher _inner;
    private readonly IServiceProvider _serviceProvider;

    public MediatRDispatcherDecorator(FranzDispatcher inner, IServiceProvider serviceProvider)
    {
      _inner = inner;
      _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
      Func<Task<TResponse>> handlerDelegate = () => _inner.Send(command, cancellationToken);

      var mediatrPipelines = _serviceProvider.GetServices<IPipelineBehavior<ICommand<TResponse>, TResponse>>().ToList();

      foreach (var pipeline in mediatrPipelines.AsEnumerable().Reverse())
      {
        var next = handlerDelegate;
        Task<TResponse> RequestHandlerDelegate(CancellationToken ct) => next();
        handlerDelegate = () => pipeline.Handle(command, RequestHandlerDelegate, cancellationToken);
      }

      return await handlerDelegate();
    }

    public async Task Send(ICommand command, CancellationToken cancellationToken = default)
    {
      Func<Task<Unit>> handlerDelegate = async () =>
      {
        await _inner.Send(command, cancellationToken);
        return Unit.Value;
      };

      var mediatrPipelines = _serviceProvider.GetServices<IPipelineBehavior<ICommand, Unit>>().ToList();

      foreach (var pipeline in mediatrPipelines.AsEnumerable().Reverse())
      {
        var next = handlerDelegate;
        Task<Unit> RequestHandlerDelegate(CancellationToken ct) => next();
        handlerDelegate = () => pipeline.Handle(command, RequestHandlerDelegate, cancellationToken);
      }

      await handlerDelegate();
    }

    public async Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
      Func<Task<TResponse>> handlerDelegate = () => _inner.Send(query, cancellationToken);

      var mediatrPipelines = _serviceProvider.GetServices<IPipelineBehavior<IQuery<TResponse>, TResponse>>().ToList();

      foreach (var pipeline in mediatrPipelines.AsEnumerable().Reverse())
      {
        var next = handlerDelegate;
        Task<TResponse> RequestHandlerDelegate(CancellationToken ct) => next();
        handlerDelegate = () => pipeline.Handle(query, RequestHandlerDelegate, cancellationToken);
      }

      return await handlerDelegate();
    }

    public Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default,
        DispatchingStrategies.PublishStrategy strategy = DispatchingStrategies.PublishStrategy.Sequential,
        DispatchingStrategies.NotificationErrorHandling errorHandling = DispatchingStrategies.NotificationErrorHandling.StopOnFirstFailure)
        where TNotification : INotification
    {
      // delegate directly to FranzDispatcher (no MediatR behaviors for notifications)
      return _inner.PublishAsync(notification, cancellationToken, strategy, errorHandling);
    }
  }
}
