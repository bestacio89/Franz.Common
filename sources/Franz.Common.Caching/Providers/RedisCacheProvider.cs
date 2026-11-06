using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Internal;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

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
        redis.call('DEL', KEYS[2])
        return nil
      end

      local time = redis.call('TIME')
      local now = tonumber(time[1]) * 1000 + math.floor(tonumber(time[2]) / 1000)
      local metadata = redis.call('GET', KEYS[2])
      local absoluteDeadline = 0
      local sliding = tonumber(ARGV[1])

      if metadata then
        local separator = string.find(metadata, ':', 1, true)
        if separator then
          absoluteDeadline = tonumber(string.sub(metadata, 1, separator - 1)) or 0
          sliding = tonumber(string.sub(metadata, separator + 1)) or 0
        end
      end

      local remaining = nil
      if absoluteDeadline > 0 then
        remaining = absoluteDeadline - now
        if remaining <= 0 then
          redis.call('DEL', KEYS[1], KEYS[2])
          return nil
        end
      end

      if sliding > 0 then
        local ttl = sliding
        if remaining and remaining < ttl then
          ttl = remaining
        end
        redis.call('PEXPIRE', KEYS[1], ttl)

        if absoluteDeadline == 0 and metadata then
          redis.call('PEXPIRE', KEYS[2], ttl)
        end
      end

      return value
      """;

  private const string WriteScript = """
      local time = redis.call('TIME')
      local now = tonumber(time[1]) * 1000 + math.floor(tonumber(time[2]) / 1000)
      local absolute = tonumber(ARGV[2])
      local sliding = tonumber(ARGV[3])
      local absoluteDeadline = 0

      redis.call('SET', KEYS[1], ARGV[1])

      local metadataTtl = 0
      if absolute > 0 then
        absoluteDeadline = now + absolute
        metadataTtl = absolute
      elseif sliding > 0 then
        metadataTtl = sliding
      end

      if metadataTtl > 0 then
        redis.call('SET', KEYS[2], tostring(absoluteDeadline) .. ':' .. tostring(sliding), 'PX', metadataTtl)
      else
        redis.call('DEL', KEYS[2])
      end

      local ttl = absolute
      if sliding > 0 and (ttl == 0 or sliding < ttl) then
        ttl = sliding
      end

      if ttl > 0 then
        redis.call('PEXPIRE', KEYS[1], ttl)
      else
        redis.call('PERSIST', KEYS[1])
      end

      return 1
      """;

  private readonly IOptionsMonitor<CacheOptions> _optionsMonitor;
  private readonly JsonSerializerOptions _serializerOptions;
  private readonly IDatabase _db;
  private readonly AsyncKeyedLock<string> _locks = new();

  public RedisCacheProvider(
      IConnectionMultiplexer connection,
      IOptionsMonitor<CacheOptions> optionsMonitor,
      IOptions<JsonSerializerOptions>? serializerOptions = null)
  {
    ArgumentNullException.ThrowIfNull(connection);
    _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    _serializerOptions = serializerOptions?.Value ?? new JsonSerializerOptions(JsonSerializerDefaults.General);
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
      // A single DEL removes both the value and its absolute-expiration metadata.
      await _db.KeyDeleteAsync(new RedisKey[] { fullKey, GetAbsoluteExpirationKey(fullKey) })
          .ConfigureAwait(false);
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
        new RedisValue[] { slidingMilliseconds }).ConfigureAwait(false);

    if (result.IsNull)
    {
      return (false, default);
    }

    var serialized = ((RedisValue)result).ToString();
    try
    {
      return (true, JsonSerializer.Deserialize(serialized, GetTypeInfo<T>()));
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

    var serialized = JsonSerializer.Serialize(value, GetTypeInfo<T>());
    var absoluteMilliseconds = options.DefaultAbsoluteExpiration > TimeSpan.Zero
        ? Math.Max(1L, (long)options.DefaultAbsoluteExpiration.TotalMilliseconds)
        : 0L;
    var slidingMilliseconds = options.DefaultSlidingExpiration > TimeSpan.Zero
        ? Math.Max(1L, (long)options.DefaultSlidingExpiration.TotalMilliseconds)
        : 0L;

    await _db.ScriptEvaluateAsync(
        WriteScript,
        new RedisKey[] { fullKey, GetAbsoluteExpirationKey(fullKey) },
        new RedisValue[] { serialized, absoluteMilliseconds, slidingMilliseconds })
        .ConfigureAwait(false);
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

  private string ApplyPrefix(string key) => $"{_optionsMonitor.CurrentValue.KeyPrefix}{key}";

  private static RedisKey GetAbsoluteExpirationKey(string fullKey)
  {
    var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullKey)));
    return $"__franz_internal__:cache-absolute:{digest}";
  }
}
