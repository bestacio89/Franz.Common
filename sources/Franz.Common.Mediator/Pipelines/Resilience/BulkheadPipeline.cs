using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;

namespace Franz.Common.Mediator.Pipelines.Resilience
{
  public class BulkheadPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
    where TRequest : notnull
  {
    private readonly SemaphoreSlim _semaphore;

    public BulkheadPipeline(BulkheadOptions options)
    {
      if (options.MaxConcurrentRequests <= 0)
        throw new ArgumentOutOfRangeException(nameof(options.MaxConcurrentRequests),
          "MaxConcurrentRequests must be greater than zero.");

      _semaphore = new SemaphoreSlim(options.MaxConcurrentRequests);
    }

    public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken)
    {
      await _semaphore.WaitAsync(cancellationToken);
      try
      {
        return await next();
      }
      finally
      {
        _semaphore.Release();
      }
    }
  }
}
