using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Internal;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Franz.Common.Caching.Providers;

/// <summary>
/// Redis-backed cache provider. Per-key factory coordination is process-local;
/// Redis itself remains the shared source of truth across application nodes.
/// </summary>
public sealed class RedisCacheProvider : ICacheProvider
{
  private const string ReadAndRefreshScript = """
      local value = redis.call('GET', KEYS[1])
      if not value then
        return nil
      end

      local absolute = redis.call('GET', KEYS[2])
      local remaining = nil
      if absolute then
        remaining = tonumber(absolute) - tonumber(ARGV[2])
        if remaining <= 0 then
          redis.call('DEL', KEYS[1], KEYS[2])
          return nil
        end
      end

      local sliding = tonumber(ARGV[1])
      if sliding > 0 then
        local ttl = sliding
        if remaining and remaining < ttl then
          ttl = remaining
        end
        redis.call('PEXPIRE', KEYS[1], ttl)
      end

      return value
      """;

  private readonly IOptionsMonitor<CacheOptions> _optionsMonitor;
  private readonly IDatabase _db;
  private readonly AsyncKeyedLock<string> _locks = new();

  public RedisCacheProvider(
      IConnectionMultiplexer connection,
      IOptionsMonitor<CacheOptions> optionsMonitor)
  {
    ArgumentNullException.ThrowIfNull(connection);
    _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    _db = connection.GetDatabase();
  }

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? requestOptions = null,
      CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentNullException.ThrowIfNull(factory);
    ct.ThrowIfCancellationRequested();

    var fullKey = ApplyPrefix(key);
    var options = requestOptions ?? _optionsMonitor.CurrentValue;

    var cached = await TryGetAsync<T>(fullKey, options, ct).ConfigureAwait(false);
    if (cached.Found)
    {
      return new CacheResult<T>(cached.Value!, IsHit: true);
    }

    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
    {
      cached = await TryGetAsync<T>(fullKey, options, ct).ConfigureAwait(false);
      if (cached.Found)
      {
        return new CacheResult<T>(cached.Value!, IsHit: true);
      }

      var value = await factory(ct).ConfigureAwait(false);
      await SetCoreAsync(fullKey, value, options, ct).ConfigureAwait(false);
      return new CacheResult<T>(value, IsHit: false);
    }
  }

  public async Task SetAsync<T>(
      string key,
      T value,
      CacheOptions? requestOptions = null,
      CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ct.ThrowIfCancellationRequested();

    var fullKey = ApplyPrefix(key);
    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
    {
      await SetCoreAsync(fullKey, value, requestOptions ?? _optionsMonitor.CurrentValue, ct)
          .ConfigureAwait(false);
    }
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ct.ThrowIfCancellationRequested();

    var fullKey = ApplyPrefix(key);
    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
    {
      var transaction = _db.CreateTransaction();
      var removeValue = transaction.KeyDeleteAsync(fullKey);
      var removeMetadata = transaction.KeyDeleteAsync(GetAbsoluteExpirationKey(fullKey));

      if (!await transaction.ExecuteAsync().ConfigureAwait(false))
      {
        throw new InvalidOperationException("Redis did not execute the cache removal transaction.");
      }

      await Task.WhenAll(removeValue, removeMetadata).ConfigureAwait(false);
    }
  }

  private async Task<(bool Found, T? Value)> TryGetAsync<T>(
      string fullKey,
      CacheOptions options,
      CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();

    // StackExchange.Redis does not expose CancellationToken overloads. Cancellation
    // is honored before issuing the command and by keyed-lock/factory waits; an
    // already-issued Redis command cannot be interrupted by this provider.
    var slidingMilliseconds = options.DefaultSlidingExpiration > TimeSpan.Zero
        ? Math.Max(1L, (long)options.DefaultSlidingExpiration.TotalMilliseconds)
        : 0L;

    var result = await _db.ScriptEvaluateAsync(
        ReadAndRefreshScript,
        new RedisKey[] { fullKey, GetAbsoluteExpirationKey(fullKey) },
        new RedisValue[] { slidingMilliseconds, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() })
        .ConfigureAwait(false);

    if (result.IsNull)
    {
      return (false, default);
    }

    var serialized = ((RedisValue)result).ToString();
    try
    {
      // A JSON null is a valid value. Found is independent of Value.
      return (true, JsonSerializer.Deserialize<T>(serialized));
    }
    catch (JsonException)
    {
      return (false, default);
    }
  }

  private async Task SetCoreAsync<T>(
      string fullKey,
      T value,
      CacheOptions options,
      CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();

    var serialized = JsonSerializer.Serialize(value);
    var absoluteExpiration = options.DefaultAbsoluteExpiration > TimeSpan.Zero
        ? DateTimeOffset.UtcNow.Add(options.DefaultAbsoluteExpiration)
        : (DateTimeOffset?)null;
    var slidingExpiration = options.DefaultSlidingExpiration > TimeSpan.Zero
        ? options.DefaultSlidingExpiration
        : (TimeSpan?)null;

    var initialExpiration = absoluteExpiration.HasValue
        ? absoluteExpiration.Value - DateTimeOffset.UtcNow
        : (TimeSpan?)null;

    if (slidingExpiration.HasValue &&
        (!initialExpiration.HasValue || slidingExpiration.Value < initialExpiration.Value))
    {
      initialExpiration = slidingExpiration;
    }

    if (initialExpiration <= TimeSpan.Zero)
    {
      initialExpiration = TimeSpan.FromMilliseconds(1);
    }

    var transaction = _db.CreateTransaction();
    var writeValue = transaction.StringSetAsync(fullKey, serialized, initialExpiration);
    Task<bool> writeMetadata;

    if (absoluteExpiration.HasValue)
    {
      var remainingAbsolute = absoluteExpiration.Value - DateTimeOffset.UtcNow;
      writeMetadata = transaction.StringSetAsync(
          GetAbsoluteExpirationKey(fullKey),
          absoluteExpiration.Value.ToUnixTimeMilliseconds(),
          remainingAbsolute > TimeSpan.Zero ? remainingAbsolute : TimeSpan.FromMilliseconds(1));
    }
    else
    {
      writeMetadata = transaction.KeyDeleteAsync(GetAbsoluteExpirationKey(fullKey));
    }

    if (!await transaction.ExecuteAsync().ConfigureAwait(false))
    {
      throw new InvalidOperationException("Redis did not execute the cache write transaction.");
    }

    await Task.WhenAll(writeValue, writeMetadata).ConfigureAwait(false);
  }

  private string ApplyPrefix(string key) => $"{_optionsMonitor.CurrentValue.KeyPrefix}{key}";

  private static RedisKey GetAbsoluteExpirationKey(string fullKey)
  {
    var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullKey)));
    return $"__franz_internal__:cache-absolute:{digest}";
  }
}
