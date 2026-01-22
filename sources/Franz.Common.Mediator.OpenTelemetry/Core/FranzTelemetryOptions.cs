namespace Franz.Common.Mediator.OpenTelemetry.Core;

public sealed class FranzTelemetryOptions
{
  // The name identifying the service in your tracing UI (Jaeger/Zipkin/Grafana)
  public string? ServiceName { get; set; }

  // Helpful for tracking deployments
  public string ServiceVersion { get; set; } = "1.0.0";

  // The name for the internal ActivitySource
  public string SourceName { get; set; } = "Franz";

  public double? SamplingRatio { get; set; } // 0.0–1.0

  public TelemetryProfile Profile { get; set; } = TelemetryProfile.Balanced;

  // The destination for OTLP data (e.g., "http://localhost:4317")
  public string? OtlpEndpoint { get; set; }

  public bool ExportToConsole { get; set; } = false;

 

}