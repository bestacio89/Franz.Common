using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Abstractions;
public class CacheEntryOptions
{
  public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(5);
  public bool Sliding { get; set; } = false;
  public CachePriority Priority { get; set; } = CachePriority.Normal;
}
