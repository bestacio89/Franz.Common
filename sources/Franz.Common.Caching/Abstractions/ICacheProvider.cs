namespace Franz.Common.Caching.Abstractions;

/// <summary>
/// Provider-neutral cache operations. Implementations must apply the same key and
/// expiration semantics for reads, writes, and removals.
/// </summary>
public interface ICacheProvider
{
  Task<CacheResult<T>> GetOrSetAsync<T>(
    string key,
    Func<CancellationToken, Task<T>> factory,
    CacheOptions? options = null,
    CancellationToken ct = default);

  /// <summary>
  /// Writes or replaces an entry. Unlike GetOrSetAsync, this operation always
  /// replaces an existing value and never invokes a factory.
  /// </summary>
  Task SetAsync<T>(
    string key,
    T value,
    CacheOptions? options = null,
    CancellationToken ct = default);

  Task RemoveAsync(string key, CancellationToken ct = default);
}
