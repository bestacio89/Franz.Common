using System.Diagnostics.CodeAnalysis;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Metrics;
using Franz.Common.Caching.Options;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System.Diagnostics;

namespace Franz.Common.Caching.Pipelines;

public sealed class CachingPipeline<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TRequest,
    TResponse> : IPipeline<TRequest, TResponse>
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
      IEnumerable<ICacheMetadataProvider> metadataProviders)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _keyStrategy = keyStrategy ?? throw new ArgumentNullException(nameof(keyStrategy));
    _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    _metadataProviders = metadataProviders ?? throw new ArgumentNullException(nameof(metadataProviders));
  }

  private static bool IsCacheableQuery()
  {
    var requestType = typeof(TRequest);
    var interfaces = requestType.GetInterfaces();

    if (typeof(ICommand).IsAssignableFrom(requestType) ||
        interfaces.Any(type => type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(ICommand<>)))
    {
      return false;
    }

    return interfaces.Any(type => type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(IQuery<>));
  }

  public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
  {
    var options = _optionsMonitor.CurrentValue;

    if (!options.Enabled || options.BypassAll || !IsCacheableQuery() ||
        _metadataProviders.Any(provider => !provider.ShouldCache(request)))
    {
      return await next().ConfigureAwait(false);
    }

    var key = _keyStrategy.BuildKey(request);
    var stopwatch = Stopwatch.StartNew();

    var customTtl = _metadataProviders
        .Select(provider => provider.GetCustomTtl(request))
        .FirstOrDefault(ttl => ttl.HasValue) ?? options.DefaultTtl;

    var result = await _cache.GetOrSetAsync(
        key,
        _ => next(),
        new CacheOptions
        {
          DefaultAbsoluteExpiration = customTtl,
          DefaultSlidingExpiration = options.DefaultSlidingExpiration
        },
        cancellationToken).ConfigureAwait(false);

    stopwatch.Stop();

    if (result.IsHit)
      CacheMetrics.Hits.Add(1);
    else
      CacheMetrics.Misses.Add(1);

    CacheMetrics.LookupLatencyMs.Record(stopwatch.Elapsed.TotalMilliseconds);

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
          stopwatch.Elapsed.TotalMilliseconds);
    }

    return result.Value!;
  }
}
