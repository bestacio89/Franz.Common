using Franz.Common.Caching.Abstractions;

public interface ICacheProvider
{
  Task<CacheResult<T>> GetOrSetAsync<T>(
    string key,
    Func<CancellationToken, Task<T>> factory,
    CacheOptions? options = null,
    CancellationToken ct = default);

  Task RemoveAsync(string key, CancellationToken ct = default);
}
