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


  // Pipeline (selective)
  public sealed class RetryPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
    where TRequest : notnull
  {
    private readonly RetryOptions _options;
    private readonly ILogger<TRequest> _logger;
    private readonly IHostEnvironment _env;

    public RetryPipeline(IOptions<RetryOptions> options, ILogger<TRequest> logger, IHostEnvironment env)
    {
      _options = options.Value;
      _logger = logger;
      _env = env;

      if (_options.MaxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(_options.MaxAttempts));
      if (_options.BaseDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(_options.BaseDelay));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken ct = default)
    {
      var attempt = 0;

      while (true)
      {
        try
        {
          return await next();
        }
        catch (Exception ex) when (
            attempt < _options.MaxAttempts - 1 &&
            !_canceled(ct, ex) &&
            (_options.ShouldRetry?.Invoke(ex) ?? false))
        {
          attempt++;
          var delay = _options.ComputeDelay?.Invoke(attempt, _options.BaseDelay) ?? _options.BaseDelay;

          if (_env.IsDevelopment() || _env.IsStaging())
          {
            _logger.LogWarning(ex,
              "Retry {Attempt}/{MaxAttempts} in {Delay} because: {Message}",
              attempt, _options.MaxAttempts, delay, ex.Message);
          }
          else if (_env.IsProduction())
          {
            // log less detail in prod
            _logger.LogWarning("Retry {Attempt}/{MaxAttempts} after {Delay}", attempt, _options.MaxAttempts, delay);
          }

          await Task.Delay(delay, ct);
        }
      }

      static bool _canceled(CancellationToken token, Exception ex) =>
          token.IsCancellationRequested || ex is OperationCanceledException;
    }
  }
}
