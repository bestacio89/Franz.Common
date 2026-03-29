using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;

namespace Franz.Common.Caching.Abstractions;

public sealed record CacheOptions
{
  public const string SectionName = "Franz:Caching";

  [SetsRequiredMembers]
  public CacheOptions() { }

  [Required]
  public required TimeSpan DefaultAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(60);

  [Required]
  public required TimeSpan DefaultSlidingExpiration { get; init; } = TimeSpan.FromMinutes(20);

  [RegularExpression(@"^[a-zA-Z0-9_-]*$", ErrorMessage = "Prefix must be alphanumeric.")]
  public string KeyPrefix { get; set; } = "franz:";

  /// <summary>
  /// Explicit connection string for Redis. 
  /// Used primarily in Test Fixtures or code-first configurations.
  /// </summary>
  public string? ConnectionString { get; set; }

  public bool EnableDistributedCache { get; init; } = true;

  public TimeSpan? LocalCacheHint { get; set; }

  public CacheItemPriority DefaultPriority { get; init; } = CacheItemPriority.Normal;

  [Range(0, long.MaxValue)]
  public long DefaultEstimatedSizeInBytes { get; init; } = 1024;
}