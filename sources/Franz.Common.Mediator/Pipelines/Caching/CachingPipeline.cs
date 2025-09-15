using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Franz.Common.Mediator.Pipelines.Caching
{
  public class CachingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
    where TRequest : notnull
  {
    private readonly IMemoryCache _cache;
    private readonly CachingOptions _options;

    public CachingPipeline(IMemoryCache cache, CachingOptions options)
    {
      _cache = cache ?? throw new ArgumentNullException(nameof(cache));
      _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
      // Key can later be customized by user (e.g., with a key generator strategy)
      var cacheKey = $"{typeof(TRequest).Name}:{request.GetHashCode()}";

      if (_cache.TryGetValue(cacheKey, out TResponse cachedResponse))
        return cachedResponse!;

      var response = await next();

      var options = new MemoryCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = _options.DefaultTtl
      };

      // Respect MaxItems if configured
      if (_options.MaxItems > 0)
      {
        // NOTE: MemoryCache doesn't have a hard cap, but we could track entries and evict manually later
        // For now, just rely on TTL; MaxItems could be handled by a custom ICacheProvider in the future
      }

      _cache.Set(cacheKey, response, options);

      return response;
    }
  }
}
