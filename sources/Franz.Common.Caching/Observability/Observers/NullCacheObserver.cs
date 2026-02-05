using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability.Observers;

public sealed class NullCacheObserver : ICacheObserver
{
  public void OnCacheHit(CacheAccessDescriptor access) { }
  public void OnCacheSet(CacheEntryDescriptor entry) { }
  public void OnCacheRemove(string key) { }
  public void OnCacheRemoveByTag(string tag) { }
}
