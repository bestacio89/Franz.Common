using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;

namespace Franz.Common.Mediator.Pipelines.Resilience
{
  public class TimeoutPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
    where TRequest : notnull
  {
    private readonly TimeSpan _timeout;

    public TimeoutPipeline(TimeoutOptions options)
    {
      if (options.Duration <= TimeSpan.Zero)
        throw new ArgumentOutOfRangeException(nameof(options.Duration),
          "Timeout duration must be greater than zero.");

      _timeout = options.Duration;
    }

    public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken)
    {
      using var timeoutCts = new CancellationTokenSource(_timeout);
      using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken, timeoutCts.Token);

      var task = next();

      if (await Task.WhenAny(task, Task.Delay(Timeout.Infinite, linkedCts.Token)) == task)
        return await task; // completed within timeout

      throw new TimeoutException(
        $"Request {typeof(TRequest).Name} exceeded timeout of {_timeout.TotalMilliseconds}ms");
    }
  }
}
