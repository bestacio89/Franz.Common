using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability;

public interface ICacheInspector
{
  IReadOnlyCollection<CacheEntrySnapshot> GetEntries();
  CacheMemorySnapshot GetMemorySnapshot();
}

