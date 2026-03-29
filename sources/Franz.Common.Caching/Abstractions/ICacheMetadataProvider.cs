namespace Franz.Common.Caching.Abstractions;

/// <summary>
/// Defines a strategy for determining caching behavior for specific requests.
/// Replaces serialized Func delegates in Options for AOT compatibility.
/// </summary>
public interface ICacheMetadataProvider
{
  /// <summary>
  /// Determines if the specific request instance should be cached.
  /// </summary>
  bool ShouldCache(object request);

  /// <summary>
  /// Provides a custom TTL for the specific request. 
  /// If null, the pipeline falls back to global MediatorCachingOptions.DefaultTtl.
  /// </summary>
  TimeSpan? GetCustomTtl(object request);
}