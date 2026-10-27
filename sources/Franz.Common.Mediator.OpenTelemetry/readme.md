# Franz.Common.Mediator.OpenTelemetry

> Application dispatch and request pipelines for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Mediator.OpenTelemetry` package provides application dispatch and request pipelines within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Mediator.OpenTelemetry
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

OpenTelemetry integration for the **Franz Framework**.
This package provides **automatic distributed tracing** across applications using the Franz ecosystem, including Mediator pipelines, HTTP calls, and custom instrumentation.

* **Current Version:** v2.3.2
* Part of the private **Franz Framework** ecosystem.

---

## 🚀 What’s New in  v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

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

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
