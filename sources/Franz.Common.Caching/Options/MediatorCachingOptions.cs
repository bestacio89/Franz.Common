using Microsoft.Extensions.Logging;

namespace Franz.Common.Caching.Options
{
  public class MediatorCachingOptions
  {
    public bool Enabled { get; set; } = true;

    /// <summary>Default TTL used when no per-request override is specified.</summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Optional predicate to decide if a specific request should be cached.</summary>
    public Func<object, bool>? ShouldCache { get; set; }

    /// <summary>Optional TTL selector per request (overrides DefaultTtl).</summary>
    public Func<object, TimeSpan?>? TtlSelector { get; set; }

    /// <summary>Log level for cache HIT entries.</summary>
    public LogLevel LogHitLevel { get; set; } = LogLevel.Debug;

    /// <summary>Log level for cache MISS entries.</summary>
    public LogLevel LogMissLevel { get; set; } = LogLevel.Information;
  }
}
