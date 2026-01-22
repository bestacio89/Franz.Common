using Franz.Common.Mediator.OpenTelemetry.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Mediator.OpenTelemetry;

public static class FranzTelemetryDefaults
{
  public static double ResolveSamplingRatio(
      IHostEnvironment env,
      FranzTelemetryOptions options)
  {
    if (options.SamplingRatio.HasValue)
      return options.SamplingRatio.Value;

    return options.Profile switch
    {
      TelemetryProfile.Full => 1.0,
      TelemetryProfile.Balanced => env.IsDevelopment() ? 1.0 : 0.1,
      TelemetryProfile.CostOptimized => env.IsDevelopment() ? 1.0 : 0.05,
      _ => 0.05
    };
  }
}
