# Franz.Common.Mediator.OpenTelemetry

> OpenTelemetry tracing for Franz Mediator requests and events, with ASP.NET Core and HttpClient instrumentation.

## Overview

This package configures OpenTelemetry tracing and registers the Mediator request and event tracing pipelines. It does not register Franz Mediator itself; register the Mediator core and its handlers separately.

## Installation

~~~bash
dotnet add package Franz.Common.Mediator.OpenTelemetry
~~~

## Registration

~~~csharp
using Franz.Common.Mediator.Extensions;

builder.Services.AddFranzMediator(
    new[] { typeof(ApplicationAssemblyMarker).Assembly });

builder.Services.AddFranzTelemetry(
    builder.Environment,
    builder.Configuration);
~~~

AddFranzTelemetry reads Franz:Telemetry, registers an ActivitySource, configures OpenTelemetry resources and sampling, adds ASP.NET Core and HttpClient instrumentation, and registers OpenTelemetryPipeline<TRequest,TResponse> and EventTracingPipeline<TEvent>.

## Configuration

~~~json
{
  "Franz": {
    "Telemetry": {
      "ServiceName": "OrdersService",
      "ServiceVersion": "3.0.0",
      "SourceName": "Franz",
      "Profile": "Balanced",
      "SamplingRatio": 0.1,
      "OtlpEndpoint": "http://localhost:4317",
      "ExportToConsole": false
    }
  }
}
~~~

Options:

- ServiceName and ServiceVersion identify the service in trace resources.
- SourceName selects the ActivitySource used by the Franz pipelines; the default is Franz.
- Profile selects Full, Balanced, or CostOptimized sampling defaults.
- SamplingRatio optionally overrides the profile/environment sampling ratio.
- OtlpEndpoint configures the OTLP exporter.
- ExportToConsole enables the console exporter.

Outside Development, OtlpEndpoint is required and registration fails when it is missing. In Development, the default endpoint is http://localhost:4317. OTLP export is always configured; the console exporter is optional.

## Scope of instrumentation

The package instruments Franz Mediator request and event execution and also enables ASP.NET Core and HttpClient instrumentation. It does not automatically configure database instrumentation, exporters for Jaeger/Zipkin, log correlation, or every custom ActivitySource in the application.

Configure additional OpenTelemetry instrumentation/exporters in the host when required. Use the same resource identity and sampling policy across the application's telemetry registrations.

## Sampling and cost

Full sampling is appropriate for controlled diagnostics, not necessarily sustained production traffic. Balanced and CostOptimized profiles reduce trace volume. Use a measured sampling ratio that matches telemetry cost and diagnostic requirements.

## License

MIT. See the repository [LICENSE](../../LICENSE).
