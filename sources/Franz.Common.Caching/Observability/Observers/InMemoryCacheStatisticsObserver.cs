using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability.Observers;

public sealed class InMemoryCacheStatisticsObserver : ICacheObserver
{
  public long Hits;
  public long Sets;
  public long Removes;

  public void OnCacheHit(CacheAccessDescriptor access) => Interlocked.Increment(ref Hits);
  public void OnCacheSet(CacheEntryDescriptor entry) => Interlocked.Increment(ref Sets);
  public void OnCacheRemove(string key) => Interlocked.Increment(ref Removes);
  public void OnCacheRemoveByTag(string tag) => Interlocked.Increment(ref Removes);
}
