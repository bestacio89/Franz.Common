using Franz.Common.Caching.Abstractions;
using StackExchange.Redis;
using System.Text.Json;


namespace Franz.Common.Caching.Redis;

public sealed class RedisCacheProvider : ICacheProvider
{
  private readonly IDatabase _db;

  private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

  public RedisCacheProvider(IConnectionMultiplexer connection)
  {
    if (connection is null)
      throw new ArgumentNullException(nameof(connection));

    _db = connection.GetDatabase();
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

    var value = await _db.StringGetAsync(key);
    if (value.HasValue)
      return JsonSerializer.Deserialize<T>(value.ToString());


    var computed = await factory(ct);

    var serialized = JsonSerializer.Serialize(computed);
    await _db.StringSetAsync(
        key,
        serialized,
        options?.Expiration ?? DefaultExpiration);

    return computed;
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    return _db.KeyDeleteAsync(key);
  }

  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
      => throw new NotSupportedException(
          "Tag-based invalidation is not supported by RedisCacheProvider.");

  private static void ValidateOptions(CacheOptions? options)
  {
    if (options is null)
      return;

    if (options.Expiration.HasValue && options.Expiration.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options.Expiration));

    if (options.LocalCacheHint is not null)
      throw new NotSupportedException(
          "LocalCacheHint is not applicable to RedisCacheProvider.");

    if (options.Tags is not null)
      throw new NotSupportedException(
          "Tags are not supported by RedisCacheProvider.");
  }
}
