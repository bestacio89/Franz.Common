using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Providers;

public sealed class MemoryCacheProvider : ICacheProvider
{
  private readonly IMemoryCache _cache;
  private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

  public MemoryCacheProvider(IMemoryCache cache)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
  }

  public async Task<T?> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? options = null,
      CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
    if (factory is null)
      throw new ArgumentNullException(nameof(factory));

    ValidateOptions(options);

    if (_cache.TryGetValue(key, out var existing))
      return (T?)existing;

    // ⚠ No stampede protection by design
    var value = await factory(ct);

    _cache.Set(
        key,
        value!,
        CreateMemoryCacheOptions(options));

    return value;
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
    _cache.Remove(key);
    return Task.CompletedTask;
  }

  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
      => throw new NotSupportedException(
          "Tag-based invalidation is not supported by MemoryCacheProvider.");

  private static MemoryCacheEntryOptions CreateMemoryCacheOptions(CacheOptions? options)
  {
    var memoryOptions = new MemoryCacheEntryOptions();

    if (options is null)
    {
      memoryOptions.AbsoluteExpirationRelativeToNow = DefaultExpiration;
      return memoryOptions;
    }

    // Priority order: AbsoluteExpirationRelativeToNow > AbsoluteExpiration > SlidingExpiration > Default
    if (options.AbsoluteExpirationRelativeToNow.HasValue)
    {
      memoryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow.Value;
    }
    else if (options.AbsoluteExpiration.HasValue)
    {
      memoryOptions.AbsoluteExpiration = options.AbsoluteExpiration.Value;
    }
    else if (options.SlidingExpiration.HasValue)
    {
      memoryOptions.SlidingExpiration = options.SlidingExpiration.Value;
    }
    else
    {
      memoryOptions.AbsoluteExpirationRelativeToNow = DefaultExpiration;
    }

    // Map priority if specified
    if (options.Priority.HasValue)
    {
      memoryOptions.Priority = options.Priority.Value switch
      {
        CacheItemPriority.Low => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Low,
        CacheItemPriority.Normal => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal,
        CacheItemPriority.High => Microsoft.Extensions.Caching.Memory.CacheItemPriority.High,
        CacheItemPriority.NeverRemove => Microsoft.Extensions.Caching.Memory.CacheItemPriority.NeverRemove,
        _ => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal
      };
    }

    // Set size if specified (for memory cache size limit enforcement)
    if (options.EstimatedSizeInBytes.HasValue)
    {
      memoryOptions.Size = options.EstimatedSizeInBytes.Value;
    }

    return memoryOptions;
  }

  private static void ValidateOptions(CacheOptions? options)
  {
    if (options is null)
      return;

    // Check AbsoluteExpirationRelativeToNow
    if (options.AbsoluteExpirationRelativeToNow.HasValue &&
        options.AbsoluteExpirationRelativeToNow.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options.AbsoluteExpirationRelativeToNow));

    // Check SlidingExpiration
    if (options.SlidingExpiration.HasValue &&
        options.SlidingExpiration.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options.SlidingExpiration));

    // Check AbsoluteExpiration
    if (options.AbsoluteExpiration.HasValue &&
        options.AbsoluteExpiration.Value <= DateTimeOffset.UtcNow)
      throw new ArgumentOutOfRangeException(nameof(options.AbsoluteExpiration));

    if (options.LocalCacheHint is not null)
      throw new NotSupportedException(
          "LocalCacheHint is not applicable to memory-only cache providers.");

    if (options.Tags is not null)
      throw new NotSupportedException(
          "Tags are not supported by MemoryCacheProvider.");
  }
}