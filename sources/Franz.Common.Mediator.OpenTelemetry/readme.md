# Franz.Common.OpenTelemetry

OpenTelemetry integration for the **Franz Framework**.
This package provides **automatic distributed tracing** across applications using the Franz ecosystem, including Mediator pipelines, HTTP calls, and custom instrumentation.

* **Current Version**: 1.7.7
* Part of the private **Franz Framework** ecosystem.

---

## 🚀 What’s New in 1.7.6

* **Self-contained OpenTelemetry configuration** — no need to call `AddOpenTelemetry()` manually in your API.
* **Automatic production-grade defaults**: OTLP exporter, sampling, instrumentation, and enriched span tags are configured internally.
* **Fail-fast OTLP enforcement in production** — prevents missing endpoint misconfiguration.
* **Seamless integration with Franz Mediator** pipelines.

---

## ⚡ Features

* **Automatic tracing** for all Mediator requests via `OpenTelemetryPipeline`.

* **Enriched context tags** for every span:

  * `franz.correlation_id`
  * `franz.user_id`
  * `franz.tenant_id`
  * `franz.culture`
  * `franz.environment`
  * `franz.metadata.*` (custom metadata values)

* **Error tagging**: exceptions automatically recorded in spans.

* **Environment-aware**: adjusts configuration between dev/test/prod automatically.

* **Logging correlation**: spans and Serilog logs share the same enrichment keys.

* **Flexible exporters**: Console (optional), OTLP, Jaeger, or Zipkin.

* **Sampling control**: configurable via `FranzTelemetryOptions` or environment defaults.

---

## 🛠 Usage

### 1. Register Franz Mediator with OpenTelemetry

```csharp
builder.Services
    .AddFranzMediator()
    .AddMediatorOpenTelemetry("Franz.Mediator");
```

That’s it. **No manual OpenTelemetry setup is required.** All exporters, sampling, and instrumentation are configured internally.

### 2. Optional Configuration via FranzTelemetryOptions

```json
"Franz": {
  "Telemetry": {
    "OtlpEndpoint": "http://otel-collector:4317",
    "ExportToConsole": false,
    "SamplingRatio": 0.5,
    "ServiceName": "MyService"
  }
}
```

* OTLP endpoint is mandatory in production — fail-fast enforced.
* Console exporter is typically used in development only.
* Sampling ratio can be tuned per environment.

---

## 📊 Example Trace

```
Name: Mediator CreateBookCommand
Kind: Internal
Status: OK
Tags:
 +- franz.correlation_id: 13a6f212-b48f-41a5-97b8-cc6dd875db94
 +- franz.user_id: 42
 +- franz.tenant_id: library-001
 +- franz.culture: en-US
 +- franz.environment: Development
 +- franz.metadata.request_size: 3kb
```

---

## 🛣 Roadmap

* Export Polly resilience metrics as OTEL counters & histograms.
* Full trace correlation between HTTP, Mediator, and DB layers.
* Prebuilt dashboards for Prometheus + Grafana.
* Unified logging + tracing observability across all Franz applications.

