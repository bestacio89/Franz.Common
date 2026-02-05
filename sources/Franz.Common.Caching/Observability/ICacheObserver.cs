namespace Franz.Common.Caching.Observability;

public interface ICacheObserver
{
  void OnCacheHit(CacheAccessDescriptor access);
  void OnCacheSet(CacheEntryDescriptor entry);
  void OnCacheRemoveByTag(string tag);
  void OnCacheRemove(string key);
}
