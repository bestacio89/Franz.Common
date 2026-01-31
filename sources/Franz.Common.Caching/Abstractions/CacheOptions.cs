using Franz.Common.Caching.Abstractions;

public sealed class CacheOptions
{
  public TimeSpan? Expiration { get; init; }
  public TimeSpan? LocalCacheHint { get; init; }
  public string[]? Tags { get; init; }
}

