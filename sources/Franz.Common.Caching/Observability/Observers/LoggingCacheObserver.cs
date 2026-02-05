using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability.Observers;

public sealed class LoggingCacheObserver : ICacheObserver
{
  private readonly ILogger<LoggingCacheObserver> _logger;

  public LoggingCacheObserver(ILogger<LoggingCacheObserver> logger)
  {
    _logger = logger;
  }

  public void OnCacheHit(CacheAccessDescriptor access)
  {

    if (!_logger.IsEnabled(LogLevel.Information))
      return;

    _logger.LogDebug(
      "Cache HIT | Key={Key} | At={Timestamp}",
      access.Key,
      access.AccessedAt);
  }

  public void OnCacheSet(CacheEntryDescriptor entry)
  {
    if (!_logger.IsEnabled(LogLevel.Information))
      return;

    _logger.LogInformation(
      "Cache SET | Key={Key} | Size={SizeBytes} bytes | TTL={Ttl}",
      entry.Key,
      entry.EstimatedSizeInBytes,
      entry.Ttl);
  }

  public void OnCacheRemove(string key)
  {
    if (!_logger.IsEnabled(LogLevel.Information))
      return;
    _logger.LogInformation(
      "Cache REMOVE | Key={Key}",
      key);
  }

  public void OnCacheRemoveByTag(string tag)
  {
    if (!_logger.IsEnabled(LogLevel.Information))
      return;
    _logger.LogInformation(
      "Cache REMOVE BY TAG | Tag={Tag}",
      tag);
  }
}