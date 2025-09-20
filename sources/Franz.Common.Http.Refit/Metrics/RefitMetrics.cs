using System.Diagnostics.Metrics;

namespace Franz.Common.Http.Refit.Metrics
{
  internal static class RefitMetrics
  {
    public const string MeterName = "Franz.Refit";
    private static readonly Meter s_meter = new(MeterName, "1.0.0");

    internal static readonly Counter<long> Requests = s_meter.CreateCounter<long>(
        "franz_refit_requests_total", "1", "Total outgoing Refit requests");

    internal static readonly Counter<long> Failures = s_meter.CreateCounter<long>(
        "franz_refit_failures_total", "1", "Failed outgoing Refit requests");

    internal static readonly Histogram<double> DurationMs = s_meter.CreateHistogram<double>(
        "franz_refit_duration_ms", "ms", "Outgoing Refit request duration (ms)");
  }
}
