using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Mediator.OpenTelemetry.Core;

public enum TelemetryProfile
{
  Full,           // 100% sampling
  Balanced,       // 5–10% typical
  CostOptimized   // 1–5% sampling
}
