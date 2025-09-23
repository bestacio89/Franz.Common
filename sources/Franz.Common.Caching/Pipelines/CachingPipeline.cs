using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Metrics;
using Franz.Common.Caching.Options;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System.Diagnostics;

namespace Franz.Common.Caching.Pipelines
{
  public class CachingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  where  TRequest : notnull
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
      _cache = cache;
      _logger = logger;
      _keyStrategy = keyStrategy;
      _options = options.Value;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
      if (!_options.Enabled)
        return await next();

      // Optional bypass per-request
      if (_options.ShouldCache != null && !_options.ShouldCache(request))
        return await next();

      var key = _keyStrategy.BuildKey(request);
      var startTs = Stopwatch.GetTimestamp();

      // ----- Lookup (HIT path) -----
      if (await _cache.ExistsAsync(key, cancellationToken))
      {
        var cached = await _cache.GetAsync<TResponse>(key, cancellationToken);
        var elapsedMs = Stopwatch.GetElapsedTime(startTs).TotalMilliseconds;

        // Metrics
        CacheMetrics.Hits.Add(1);
        CacheMetrics.LookupLatencyMs.Record(elapsedMs);

        // Traces
        Activity.Current?.SetTag("franz.cache.key", key);
        Activity.Current?.SetTag("franz.cache.hit", true);

        // Logs
        using (LogContext.PushProperty("FranzCorrelationId", MediatorContext.Current?.CorrelationId))
        using (LogContext.PushProperty("FranzPipeline", nameof(CachingPipeline<TRequest, TResponse>)))
        using (LogContext.PushProperty("FranzCacheKey", key))
        using (LogContext.PushProperty("FranzCacheHit", true))
        {
          _logger.Log(_options.LogHitLevel, "Cache HIT for {RequestType} in {Elapsed}ms",
            typeof(TRequest).Name, elapsedMs);
        }

        return cached!;
      }

      // ----- Miss -> execute -----
      var missElapsedMs = Stopwatch.GetElapsedTime(startTs).TotalMilliseconds;
      CacheMetrics.Misses.Add(1);
      CacheMetrics.LookupLatencyMs.Record(missElapsedMs);
      Activity.Current?.SetTag("franz.cache.key", key);
      Activity.Current?.SetTag("franz.cache.hit", false);

      using (LogContext.PushProperty("FranzCorrelationId", MediatorContext.Current?.CorrelationId))
      using (LogContext.PushProperty("FranzPipeline", nameof(CachingPipeline<TRequest, TResponse>)))
      using (LogContext.PushProperty("FranzCacheKey", key))
      using (LogContext.PushProperty("FranzCacheHit", false))
      {
        _logger.Log(_options.LogMissLevel, "Cache MISS for {RequestType} in {Elapsed}ms",
          typeof(TRequest).Name, missElapsedMs);
      }

      var response = await next();

      // Determine TTL (allow per-request override)
      var ttl = _options.TtlSelector?.Invoke(request) ?? _options.DefaultTtl;

      await _cache.SetAsync(key, response, ttl, cancellationToken);

      _logger.LogInformation("Cache SET for {RequestType} with TTL {TtlSeconds}s",
        typeof(TRequest).Name, ttl.TotalSeconds);

      Activity.Current?.SetTag("franz.cache.ttl_seconds", ttl.TotalSeconds);

      return response;
    }
  }
}
