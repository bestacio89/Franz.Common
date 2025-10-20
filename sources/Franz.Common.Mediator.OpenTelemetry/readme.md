# Franz.Common.Mediator.OpenTelemetry

OpenTelemetry integration for the **Franz Framework**.
This package adds **automatic distributed tracing** to all Mediator requests, with the same **enriched context** you already see in Franz logging pipelines.

- **Current Version**: 1.6.15

---

## ? Features

* ?? **Automatic tracing** for every Mediator request via `OpenTelemetryPipeline`.
* ?? **Enriched tags out of the box**:

  * `franz.correlation_id`
  * `franz.user_id`
  * `franz.tenant_id`
  * `franz.culture`
  * `franz.environment`
  * `franz.metadata.*` (custom values)
* ? **Seamless error tagging**: exception type & message recorded in spans.
* ?? **Environment-aware**: uses `IHostEnvironment` for runtime environment.
* ?? **Plug-and-play**: single extension method `AddMediatorOpenTelemetry()`.
* ?? **Consistent with Serilog logging**: logs and traces share the same enrichment keys for easy correlation.

---

## ?? Installation

```powershell
dotnet add package Franz.Common.Mediator.OpenTelemetry --version 1.3.15
```

---

## ?? Usage

### 1. Register OpenTelemetry in your app

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracer =>
    {
        tracer
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Franz.Mediator") // important: matches the ActivitySource
            .AddConsoleExporter();      // or Jaeger/Zipkin/OTEL collector
    });
```

### 2. Register Franz Mediator with OpenTelemetry

```csharp
builder.Services
    .AddFranzMediator()
    .AddMediatorOpenTelemetry("Franz.Mediator");
```

### 3. Enjoy automatic spans ??

Every Mediator request will produce an OpenTelemetry activity with enriched tags.

---

## ?? Example Trace

A sample span in Jaeger/Zipkin:

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

## ??? Roadmap

* ?? Export Polly resilience metrics as OTEL counters & histograms.
* ?? Trace correlation between HTTP, Mediator, and DB layers.
* ?? Prebuilt dashboards for Prometheus + Grafana.


