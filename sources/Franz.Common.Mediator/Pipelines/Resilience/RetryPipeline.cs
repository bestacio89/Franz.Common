using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Resilience
{
  public class RetryPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
    where TRequest : notnull
  {
    private readonly int _maxAttempts;
    private readonly TimeSpan _delay;

    public RetryPipeline(RetryOptions options)
    {
      if (options.MaxAttempts <= 0)
        throw new ArgumentOutOfRangeException(nameof(options.MaxAttempts),
          "MaxAttempts must be greater than zero.");

      if (options.Delay < TimeSpan.Zero)
        throw new ArgumentOutOfRangeException(nameof(options.Delay),
          "Delay must be a non-negative duration.");

      _maxAttempts = options.MaxAttempts;
      _delay = options.Delay;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
      var attempt = 0;

      while (true)
      {
        try
        {
          return await next();
        }
        catch when (attempt < _maxAttempts - 1) // allow retries until last attempt
        {
          attempt++;
          await Task.Delay(_delay, cancellationToken);
        }
      }
    }
  }
}
