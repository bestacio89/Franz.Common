using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Resilience
{
  public class TimeoutPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
      where TRequest : notnull
  {
    private readonly TimeoutOptions _options;
    private readonly ILogger<TRequest> _logger;
    private readonly IHostEnvironment _env;

    public TimeoutPipeline(
        IOptions<TimeoutOptions> options,
        ILogger<TRequest> logger,
        IHostEnvironment env)
    {
      _options = options.Value;
      _logger = logger;
      _env = env;

      if (_options.Duration <= TimeSpan.Zero && !_options.Disabled)
        throw new ArgumentOutOfRangeException(nameof(_options.Duration),
            "Timeout duration must be greater than zero.");
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
      if (_options.Disabled)
      {
        _logger.LogInformation("TimeoutPipeline disabled for {Request}", typeof(TRequest).Name);
        return await next();
      }

      using var timeoutCts = new CancellationTokenSource(_options.Duration);
      using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
          cancellationToken, timeoutCts.Token);

      var task = next();

      if (await Task.WhenAny(task, Task.Delay(Timeout.Infinite, linkedCts.Token)) == task)
        return await task; // completed within timeout

      var message = $"Request {typeof(TRequest).Name} exceeded timeout of {_options.Duration.TotalMilliseconds}ms";

      if (_options.VerboseLogging || _env.IsDevelopment())
      {
        _logger.LogError(message);
      }
      else if (_env.IsProduction())
      {
        _logger.LogWarning("Request {Request} timed out", typeof(TRequest).Name);
      }

      throw new TimeoutException(message);
    }
  }
}
