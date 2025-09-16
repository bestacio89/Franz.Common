using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;

namespace Franz.Common.Mediator.Pipelines.Resilience
{
  public class CircuitBreakerPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
    where TRequest : notnull
  {
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;

    private int _failureCount = 0;
    private DateTime? _circuitOpened = null;
    private readonly object _lock = new();

    public CircuitBreakerPipeline(CircuitBreakerOptions options)
    {
      if (options.FailureThreshold <= 0)
        throw new ArgumentOutOfRangeException(nameof(options.FailureThreshold),
          "FailureThreshold must be greater than zero.");

      if (options.OpenDuration <= TimeSpan.Zero)
        throw new ArgumentOutOfRangeException(nameof(options.OpenDuration),
          "OpenDuration must be greater than zero.");

      _failureThreshold = options.FailureThreshold;
      _openDuration = options.OpenDuration;
    }

    public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken)
    {
      lock (_lock)
      {
        if (_circuitOpened.HasValue && DateTime.UtcNow < _circuitOpened.Value.Add(_openDuration))
        {
          throw new InvalidOperationException(
            $"Circuit breaker is OPEN for {typeof(TRequest).Name}, try again later.");
        }
      }

      try
      {
        var response = await next();

        lock (_lock)
        {
          _failureCount = 0;
          _circuitOpened = null;
        }

        return response;
      }
      catch
      {
        lock (_lock)
        {
          _failureCount++;
          if (_failureCount >= _failureThreshold)
            _circuitOpened = DateTime.UtcNow;
        }

        throw;
      }
    }
  }
}
