namespace Franz.Common.Caching.Abstractions;

/// <summary>
/// Provider-neutral cache lookup and removal operations.
/// </summary>
public interface ICacheProvider
{
  Task<CacheResult<T>> GetOrSetAsync<T>(
    string key,
    Func<CancellationToken, Task<T>> factory,
    CacheOptions? options = null,
    CancellationToken ct = default);

  Task RemoveAsync(string key, CancellationToken ct = default);
}
