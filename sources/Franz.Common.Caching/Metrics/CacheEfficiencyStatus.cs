using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Metrics;

public enum CacheEfficiencyStatus
{
  Excellent,
  Acceptable,
  Useless,     // Set mais jamais relu
  Suspicious   // Trop peu de hits
}

