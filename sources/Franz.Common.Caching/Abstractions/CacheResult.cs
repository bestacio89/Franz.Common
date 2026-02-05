namespace Franz.Common.Caching.Abstractions;

public readonly record struct CacheResult<T>(
  T Value,
  bool IsHit
);
