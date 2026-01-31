using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Metrics;
using Franz.Common.Caching.Options;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System.Diagnostics;

namespace Franz.Common.Caching.Pipelines;

public sealed class CachingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
  private readonly ICacheProvider _cache;
  private readonly ILogger<CachingPipeline<TRequest, TResponse>> _logger;
  private readonly ICacheKeyStrategy _keyStrategy;
  private readonly MediatorCachingOptions _options;

  public CachingPipeline(
      ICacheProvider cache,
      IOptions<MediatorCachingOptions> options,
      ICacheKeyStrategy keyStrategy,
      ILogger<CachingPipeline<TRequest, TResponse>> logger)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _keyStrategy = keyStrategy ?? throw new ArgumentNullException(nameof(keyStrategy));
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
  }

  public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
  {
    if (!_options.Enabled || (_options.ShouldCache != null && !_options.ShouldCache(request)))
    {
      return await next();
    }

    var key = _keyStrategy.BuildKey(request);
    var sw = Stopwatch.StartNew();

    // Use GetOrSetAsync to avoid race condition
    var response = await _cache.GetOrSetAsync(
        key,
        async ct =>
        {
          var resp = await next();
          return resp;
        },
        new Franz.Common.Caching.Abstractions.CacheOptions
        {
          Expiration = _options.TtlSelector?.Invoke(request) ?? _options.DefaultTtl
        },
        cancellationToken
    );

    sw.Stop();

    var isHit = response is not null; // if cache was already populated, factory not invoked
    if (isHit)
      CacheMetrics.Hits.Add(1);
    else
      CacheMetrics.Misses.Add(1);

    CacheMetrics.LookupLatencyMs.Record(sw.Elapsed.TotalMilliseconds);

    using (LogContext.PushProperty("FranzCorrelationId", MediatorContext.Current?.CorrelationId))
    using (LogContext.PushProperty("FranzPipeline", nameof(CachingPipeline<TRequest, TResponse>)))
    using (LogContext.PushProperty("FranzCacheKey", key))
    using (LogContext.PushProperty("FranzCacheHit", isHit))
    {
      _logger.Log(
          isHit ? _options.LogHitLevel : _options.LogMissLevel,
          "Cache {HitOrMiss} for {RequestType} in {Elapsed}ms",
          isHit ? "HIT" : "MISS",
          typeof(TRequest).Name,
          sw.Elapsed.TotalMilliseconds
      );
    }

    return response!;
  }
}
