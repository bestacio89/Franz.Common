using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Metrics;

public class CacheEntryStats
{
  public long Hits;
  public long Sets;
  public long EstimatedSizeBytes;
  public List<string> Tags = new(); // optional, if using tags
  public DateTime LastSet;
  public DateTime LastAccess;
}
