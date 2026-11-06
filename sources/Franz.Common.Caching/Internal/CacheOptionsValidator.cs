using Franz.Common.Caching.Abstractions;

namespace Franz.Common.Caching.Internal;

internal static class CacheOptionsValidator
{
  private static readonly TimeSpan MaximumCacheLifetime = TimeSpan.FromDays(36500);

  public static bool IsValid(CacheOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    return IsValidLifetime(options.DefaultAbsoluteExpiration) &&
           IsValidLifetime(options.DefaultSlidingExpiration) &&
           (!options.LocalCacheHint.HasValue || IsValidLifetime(options.LocalCacheHint.Value)) &&
           options.DefaultEstimatedSizeInBytes >= 0 &&
           Enum.IsDefined(options.DefaultPriority) &&
           IsValidKeyPrefix(options.KeyPrefix);
  }

  public static bool IsValidLifetime(TimeSpan value)
      => value >= TimeSpan.Zero && value <= MaximumCacheLifetime;

  public static void ThrowIfInvalid(CacheOptions options)
  {
    if (!IsValid(options))
    {
      throw new ArgumentException(
          "Cache options contain an invalid expiration, key prefix, priority, or estimated size.",
          nameof(options));
    }
  }

  private static bool IsValidKeyPrefix(string? value)
  {
    if (value is null) return false;

    foreach (var character in value)
    {
      if (!char.IsAsciiLetterOrDigit(character) && character is not ':' and not '_' and not '-')
        return false;
    }

    return true;
  }
}
