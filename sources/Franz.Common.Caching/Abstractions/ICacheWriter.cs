namespace Franz.Common.Caching.Abstractions;

/// <summary>
/// Optional cache write capability for providers that support unconditional
/// replacement. Kept separate from ICacheProvider to preserve compatibility
/// with existing provider implementations.
/// </summary>
public interface ICacheWriter
{
  Task SetAsync<T>(
    string key,
    T value,
    CacheOptions? options = null,
    CancellationToken ct = default);
}
