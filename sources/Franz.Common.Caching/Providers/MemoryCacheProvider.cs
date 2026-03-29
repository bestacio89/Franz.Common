using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Franz.Common.Caching.Providers;

public sealed class MemoryCacheProvider : ICacheProvider
{
  private readonly IMemoryCache _cache;
  private readonly IOptionsMonitor<CacheOptions> _optionsMonitor;

  public MemoryCacheProvider(
      IMemoryCache cache,
      IOptionsMonitor<CacheOptions> optionsMonitor)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
  }

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? requestOptions = null,
      CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    if (factory is null)
      throw new ArgumentNullException(nameof(factory));

    // 🔹 1. Try cache
    if (_cache.TryGetValue(key, out var existing))
    {
      return new CacheResult<T>((T)existing!, IsHit: true);
    }

    // 🔹 2. Cache miss → compute
    var value = await factory(ct);

    // 🔹 3. Create entry options using Reactive Monitor for defaults
    var entryOptions = CreateMemoryCacheOptions(requestOptions);

    _cache.Set(key, value!, entryOptions);

    return new CacheResult<T>(value, IsHit: false);
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    _cache.Remove(key);
    return Task.CompletedTask;
  }

  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
      => throw new NotSupportedException("Tag-based invalidation is not supported by MemoryCacheProvider.");

  private MemoryCacheEntryOptions CreateMemoryCacheOptions(CacheOptions? requestOptions)
  {
    var globalSettings = _optionsMonitor.CurrentValue;
    var memoryOptions = new MemoryCacheEntryOptions();

    // Use global monitor defaults if no specific request options provided
    if (requestOptions is null)
    {
      memoryOptions.AbsoluteExpirationRelativeToNow = globalSettings.DefaultAbsoluteExpiration;
      memoryOptions.SlidingExpiration = globalSettings.DefaultSlidingExpiration;
      memoryOptions.Priority = globalSettings.DefaultPriority;
      return memoryOptions;
    }

    // Map request-specific overrides with reactive fallbacks
    memoryOptions.AbsoluteExpirationRelativeToNow = requestOptions.DefaultAbsoluteExpiration;
    memoryOptions.SlidingExpiration = requestOptions.DefaultSlidingExpiration;
    memoryOptions.Priority = requestOptions.DefaultPriority;

    if (requestOptions.DefaultEstimatedSizeInBytes!=0)
    {
      memoryOptions.Size = requestOptions.DefaultEstimatedSizeInBytes;
    }

    return memoryOptions;
  }
}