using Franz.Common.Caching.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability;

public sealed class ObservableCacheProvider : ICacheProvider
{
  private readonly ICacheProvider _inner;
  private readonly ICacheObserver _observer;

  public ObservableCacheProvider(
      ICacheProvider inner,
      ICacheObserver observer)
  {
    _inner = inner;
    _observer = observer;
  }

  public async Task<T?> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? options = null,
      CancellationToken ct = default)
  {
    var value = await _inner.GetOrSetAsync(key, factory, options, ct);

    // Observer calls happen inside inner provider
    // OR are inferred depending on implementation

    return value;
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    await _inner.RemoveAsync(key, ct);
    _observer.OnCacheRemove(key);
  }

  public async Task RemoveByTagAsync(string tag, CancellationToken ct = default)
  {
    await _inner.RemoveByTagAsync(tag, ct);
    _observer.OnCacheRemoveByTag(tag);
  }
}
