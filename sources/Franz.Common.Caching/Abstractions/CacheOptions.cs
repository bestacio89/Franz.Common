using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;

namespace Franz.Common.Caching.Abstractions;

public sealed record CacheOptions
{
  public const string SectionName = "Caching";

  [SetsRequiredMembers]
  public CacheOptions() { }

  public required TimeSpan DefaultAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(60);

  public required TimeSpan DefaultSlidingExpiration { get; init; } = TimeSpan.FromMinutes(20);

  [RegularExpression(
      @"^[a-zA-Z0-9:_-]*$",
      ErrorMessage = "Prefix may contain only letters, digits, colon, underscore, and hyphen.")]
  public string KeyPrefix { get; set; } = "franz:";

  /// <summary>
  /// Redis connection string. Treat this value as a secret; ToString redacts it.
  /// </summary>
  public string? ConnectionString { get; set; }

  public bool EnableDistributedCache { get; init; } = true;

  public TimeSpan? LocalCacheHint { get; set; }

  public CacheItemPriority DefaultPriority { get; init; } = CacheItemPriority.Normal;

  [Range(0, long.MaxValue)]
  public long DefaultEstimatedSizeInBytes { get; init; } = 1024;

  public override string ToString()
      => $"{nameof(CacheOptions)} {{ " +
         $"DefaultAbsoluteExpiration = {DefaultAbsoluteExpiration}, " +
         $"DefaultSlidingExpiration = {DefaultSlidingExpiration}, " +
         $"KeyPrefix = {KeyPrefix}, " +
         $"ConnectionString = {(ConnectionString is null ? "null" : "[REDACTED]")}, " +
         $"EnableDistributedCache = {EnableDistributedCache}, " +
         $"LocalCacheHint = {LocalCacheHint}, " +
         $"DefaultPriority = {DefaultPriority}, " +
         $"DefaultEstimatedSizeInBytes = {DefaultEstimatedSizeInBytes} }}";
}
