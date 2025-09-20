using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Resilience
{
  public class BulkheadPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
      where TRequest : notnull
  {
    private readonly BulkheadOptions _options;
    private readonly ILogger<TRequest> _logger;
    private readonly IHostEnvironment _env;
    private readonly SemaphoreSlim _semaphore;

    public BulkheadPipeline(BulkheadOptions options, ILogger<TRequest> logger, IHostEnvironment env)
    {
      if (options.MaxConcurrentRequests <= 0)
        throw new ArgumentOutOfRangeException(nameof(options.MaxConcurrentRequests),
            "MaxConcurrentRequests must be greater than zero.");

      _options = options;
      _logger = logger;
      _env = env;

      _semaphore = new SemaphoreSlim(options.MaxConcurrentRequests);
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
      if (_options.Disabled)
      {
        _logger.LogInformation("BulkheadPipeline disabled for {Request}", typeof(TRequest).Name);
        return await next();
      }

      // Check if there is an available slot within a specified timeout.
      // If MaxQueueLength is set, we use it to enforce a queue limit.
      var didEnter = await _semaphore.WaitAsync(_options.MaxQueueLength.HasValue ? TimeSpan.Zero : Timeout.InfiniteTimeSpan, cancellationToken);

      if (!didEnter)
      {
        _logger.LogWarning("Bulkhead rejected request {Request} — max queue length reached",
            typeof(TRequest).Name);
        throw new InvalidOperationException(
            $"Bulkhead queue limit reached for {typeof(TRequest).Name}");
      }

      try
      {
        if (_options.VerboseLogging || _env.IsDevelopment())
        {
          _logger.LogInformation("Executing {Request} inside bulkhead (Available slots: {Slots})",
              typeof(TRequest).Name, _semaphore.CurrentCount);
        }

        return await next();
      }
      finally
      {
        _semaphore.Release();
      }
    }
  }
}