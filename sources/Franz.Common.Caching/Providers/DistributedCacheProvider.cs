using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Caching.Distributed;

public sealed class DistributedCacheProvider : ICacheProvider, ICacheWriter, IDisposable
{
  private readonly IDistributedCache _cache;
  private readonly IOptionsMonitor<CacheOptions> _optionsMonitor;
  private readonly JsonSerializerOptions _serializerOptions;
  private readonly AsyncKeyedLock<string> _locks = new();
  private int _disposed;

  public DistributedCacheProvider(IDistributedCache cache, IOptionsMonitor<CacheOptions> optionsMonitor)
      : this(cache, optionsMonitor, null) { }

  public DistributedCacheProvider(
      IDistributedCache cache,
      IOptionsMonitor<CacheOptions> optionsMonitor,
      IOptions<JsonSerializerOptions>? serializerOptions)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    _serializerOptions = serializerOptions?.Value ?? new JsonSerializerOptions(JsonSerializerDefaults.General);
  }

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key, Func<CancellationToken, Task<T>> factory,
      CacheOptions? requestOptions = null, CancellationToken ct = default)
  {
    ThrowIfDisposed();
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentNullException.ThrowIfNull(factory);
    ct.ThrowIfCancellationRequested();

    var effectiveOptions = requestOptions ?? _optionsMonitor.CurrentValue;
    CacheOptionsValidator.ThrowIfInvalid(effectiveOptions);
    var fullKey = ApplyPrefix(key);
    var cached = await _cache.GetStringAsync(fullKey, ct).ConfigureAwait(false);
    if (TryDeserialize(cached, out T? result))
      return new CacheResult<T>(result!, IsHit: true);

    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
    {
      cached = await _cache.GetStringAsync(fullKey, ct).ConfigureAwait(false);
      if (TryDeserialize(cached, out result))
        return new CacheResult<T>(result!, IsHit: true);

      var value = await factory(ct).ConfigureAwait(false);
      await SetCoreAsync(fullKey, value, effectiveOptions, ct).ConfigureAwait(false);
      return new CacheResult<T>(value, IsHit: false);
    }
  }

  public async Task SetAsync<T>(
      string key, T value, CacheOptions? requestOptions = null, CancellationToken ct = default)
  {
    ThrowIfDisposed();
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ct.ThrowIfCancellationRequested();
    var effectiveOptions = requestOptions ?? _optionsMonitor.CurrentValue;
    CacheOptionsValidator.ThrowIfInvalid(effectiveOptions);
    var fullKey = ApplyPrefix(key);
    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
      await SetCoreAsync(fullKey, value, effectiveOptions, ct).ConfigureAwait(false);
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    ThrowIfDisposed();
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ct.ThrowIfCancellationRequested();
    var fullKey = ApplyPrefix(key);
    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
      await _cache.RemoveAsync(fullKey, ct).ConfigureAwait(false);
  }

  public void Dispose() => Interlocked.Exchange(ref _disposed, 1);

  private void ThrowIfDisposed()
  {
    if (Volatile.Read(ref _disposed) != 0)
      throw new ObjectDisposedException(nameof(DistributedCacheProvider));
  }

  private string ApplyPrefix(string key) => $"{_optionsMonitor.CurrentValue.KeyPrefix}{key}";

  private async Task SetCoreAsync<T>(string fullKey, T value, CacheOptions options, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    var json = JsonSerializer.Serialize(value, GetTypeInfo<T>());
    ct.ThrowIfCancellationRequested();
    await _cache.SetStringAsync(fullKey, json, CreateDistributedCacheOptions(options), ct)
        .ConfigureAwait(false);
  }

  private bool TryDeserialize<T>(string? cached, out T? result)
  {
    result = default;
    if (cached is null) return false;
    try
    {
      result = JsonSerializer.Deserialize(cached, GetTypeInfo<T>());
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private JsonTypeInfo<T> GetTypeInfo<T>()
  {
    try
    {
      return (JsonTypeInfo<T>)_serializerOptions.GetTypeInfo(typeof(T));
    }
    catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException)
    {
      throw new InvalidOperationException(
          $"No JSON metadata is registered for cached value type '{typeof(T).FullName}'. " +
          "Register a source-generated JsonSerializerContext with AddFranzCachingJsonTypeInfoResolver.",
          exception);
    }
  }

  private static DistributedCacheEntryOptions CreateDistributedCacheOptions(CacheOptions options)
  {
    return new DistributedCacheEntryOptions
    {
      AbsoluteExpirationRelativeToNow = options.DefaultAbsoluteExpiration > TimeSpan.Zero
          ? options.DefaultAbsoluteExpiration : null,
      SlidingExpiration = options.DefaultSlidingExpiration > TimeSpan.Zero
          ? options.DefaultSlidingExpiration : null
    };
  }
}
