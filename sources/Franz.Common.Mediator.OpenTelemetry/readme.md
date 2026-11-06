# Franz.Common.Mediator.OpenTelemetry

> OpenTelemetry tracing integration for Franz Mediator request and event pipelines.

## Overview

The package configures an `ActivitySource`, OpenTelemetry resources, ASP.NET Core and HttpClient instrumentation, OTLP export, and Mediator tracing pipelines.

## Installation

```bash
dotnet add package Franz.Common.Mediator.OpenTelemetry
```

## Registration

```csharp
builder.Services.AddFranzTelemetry(
    builder.Environment,
    builder.Configuration);
```

The extension reads `Franz:Telemetry` and registers:

- An `ActivitySource` using the configured source name.
- OpenTelemetry service resource attributes and environment name.
- ASP.NET Core and HttpClient instrumentation.
- OTLP export.
- Optional console export.
- Mediator request and event tracing pipelines.

In Development, an omitted OTLP endpoint defaults to `http://localhost:4317`. In non-Development environments, `Franz:Telemetry:OtlpEndpoint` is required and registration fails if it is missing.

## Configuration

```json
{
  "Franz": {
    "Telemetry": {
      "ServiceName": "orders-api",
      "ServiceVersion": "3.0.0",
      "SourceName": "Franz",
      "SamplingRatio": 0.2,
      "Profile": "Balanced",
      "OtlpEndpoint": "http://otel-collector:4317",
      "ExportToConsole": false
    }
  }
}
```

Sampling is resolved from the environment and telemetry profile through `FranzTelemetryDefaults`; use an explicit ratio only when the deployment has a defined sampling policy. The exporter sends traces to the configured OTLP endpoint.

## Manual instrumentation

Resolve the registered `ActivitySource` and create activities for application operations that need explicit spans. Use stable, low-cardinality activity names and tags; do not put secrets or message payloads into telemetry attributes.

## License

MIT. See the repository [LICENSE](../../LICENSE).
