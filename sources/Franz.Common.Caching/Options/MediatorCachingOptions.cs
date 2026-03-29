using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Caching.Options;

public sealed record MediatorCachingOptions
{
  public const string SectionName = "Franz:Mediator:Caching";

  [SetsRequiredMembers]
  public MediatorCachingOptions() { }

  public bool Enabled { get; init; } = true;

  /// <summary>
  /// Global toggle to bypass caching for all requests.
  /// </summary>
  public bool BypassAll { get; init; } = false;

  /// <summary>
  /// Default Absolute TTL for mediator requests.
  /// </summary>
  [Required]
  public required TimeSpan DefaultTtl { get; init; } = TimeSpan.FromMinutes(5);

  /// <summary>
  /// Default Sliding TTL for mediator requests.
  /// </summary>
  [Required]
  public required TimeSpan DefaultSlidingExpiration { get; init; } = TimeSpan.FromMinutes(2);

  public LogLevel LogHitLevel { get; init; } = LogLevel.Debug;
  public LogLevel LogMissLevel { get; init; } = LogLevel.Information;
}