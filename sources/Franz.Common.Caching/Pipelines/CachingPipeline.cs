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
  private readonly IOptionsMonitor<MediatorCachingOptions> _optionsMonitor;
  private readonly IEnumerable<ICacheMetadataProvider> _metadataProviders;

  public CachingPipeline(
      ICacheProvider cache,
      IOptionsMonitor<MediatorCachingOptions> optionsMonitor,
      ICacheKeyStrategy keyStrategy,
      ILogger<CachingPipeline<TRequest, TResponse>> logger,
      IEnumerable<ICacheMetadataProvider> metadataProviders) // Strategy injection
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _keyStrategy = keyStrategy ?? throw new ArgumentNullException(nameof(keyStrategy));
    _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    _metadataProviders = metadataProviders;
  }

  public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
  {
    var options = _optionsMonitor.CurrentValue;

    // 1. Check Global Toggle & Strategy-based "ShouldCache"
    if (!options.Enabled || options.BypassAll || _metadataProviders.Any(p => !p.ShouldCache(request)))
    {
      return await next();
    }

    var key = _keyStrategy.BuildKey(request);
    var sw = Stopwatch.StartNew();

    // 2. Resolve TTL from Strategy or Fallback to Options Default
    var customTtl = _metadataProviders
        .Select(p => p.GetCustomTtl(request))
        .FirstOrDefault(t => t.HasValue) ?? options.DefaultTtl;

    // 3. Execution via Reactive Cache Provider
    var result = await _cache.GetOrSetAsync(
        key,
        async ct => await next(),
        new CacheOptions
        {
          DefaultAbsoluteExpiration = customTtl,
          DefaultSlidingExpiration = options.DefaultSlidingExpiration,
        },
        cancellationToken
    );

    sw.Stop();

    // 🔹 Metrics (Native .NET 10 System.Diagnostics.Metrics)
    if (result.IsHit)
      CacheMetrics.Hits.Add(1);
    else
      CacheMetrics.Misses.Add(1);

    CacheMetrics.LookupLatencyMs.Record(sw.Elapsed.TotalMilliseconds);

    // 🔹 Structured Logging with Reactive Log Levels
    using (LogContext.PushProperty("FranzCorrelationId", MediatorContext.Current?.CorrelationId))
    using (LogContext.PushProperty("FranzPipeline", nameof(CachingPipeline<TRequest, TResponse>)))
    using (LogContext.PushProperty("FranzCacheKey", key))
    using (LogContext.PushProperty("FranzCacheHit", result.IsHit))
    {
      _logger.Log(
          result.IsHit ? options.LogHitLevel : options.LogMissLevel,
          "Cache {HitOrMiss} for {RequestType} in {Elapsed}ms",
          result.IsHit ? "HIT" : "MISS",
          typeof(TRequest).Name,
          sw.Elapsed.TotalMilliseconds
      );
    }

    return result.Value!;
  }
}