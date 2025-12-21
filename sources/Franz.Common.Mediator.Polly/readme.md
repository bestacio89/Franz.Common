# **Franz.Common.Mediator.Polly**

**Franz.Common.Mediator.Polly** extends [Franz.Common.Mediator](https://www.nuget.org/packages/Franz.Common.Mediator/) with **Polly-based resilience pipelines**.
It gives you **retry, circuit breaker, advanced circuit breaker, timeout, and bulkhead isolation** for Mediator requests — all with **enriched Serilog logging, context-awareness, and resilience telemetry** built-in.

> ⚡ No extra wiring. No boilerplate. Just plug it in.

---

* **Current Version**: 1.7.2
* **Target Frameworks**: .NET 9+
* **Dependencies**: `Polly`, `Serilog`, `Franz.Common.Mediator`
- Part of the private **Franz Framework** ecosystem.
---

## ✨ Features

* 🔁 **Retry Pipeline** — automatic retries with backoff & correlated telemetry.
* 🚦 **Circuit Breaker Pipeline** — prevents cascading failures under load.
* 📊 **Advanced Circuit Breaker Pipeline** — trips based on failure ratio in rolling window.
* ⏱ **Timeout Pipeline** — cancels long-running requests automatically.
* 📦 **Bulkhead Pipeline** — limits concurrent requests and queue pressure.
* 🧠 **ResilienceContext** — shared state across all resilience pipelines:

  * `RetryCount`, `CircuitOpen`, `TimeoutOccurred`, `BulkheadRejected`, `Duration`
* 👁 **IResilienceObserver** — extensibility hooks for external telemetry, alerts, or dashboards.
* 📝 **Enriched Serilog Logging** — all logs include:

  * Correlation ID
  * Request type
  * Policy name
  * Pipeline name
  * Execution time
  * Health indicators

---

## 📦 Installation

```bash
dotnet add package Franz.Common.Mediator.Polly
```

---

## ⚙️ Setup

### 1. Config-driven Entry Point (v1.6.2+)

From **v1.6.2**, resilience is now **config-driven**.
Define your policies in `appsettings.json`:

```json
"Resilience": {
  "RetryPolicy": {
    "Enabled": true,
    "RetryCount": 3,
    "RetryIntervalMilliseconds": 500
  },
  "CircuitBreaker": {
    "Enabled": true,
    "FailureThreshold": 0.5,
    "MinimumThroughput": 10,
    "DurationOfBreakSeconds": 30
  },
  "TimeoutPolicy": {
    "Enabled": true,
    "TimeoutSeconds": 5
  },
  "BulkheadPolicy": {
    "Enabled": true,
    "MaxParallelization": 10,
    "MaxQueueSize": 20
  }
}
```

Then just call:

```csharp
builder.Services.AddFranzResilience(builder.Configuration);
```

✅ That’s it — retry, circuit breaker, timeout, and bulkhead are auto-registered from config and wired into Mediator pipelines.
✅ Each policy injects structured logs and updates the `ResilienceContext`.

---

### 2. Manual Registration (pre-1.6.2 style)

If you prefer explicit registration:

```csharp
using Franz.Common.Mediator.Polly;

builder.Services.AddFranzPollyPolicies(options =>
{
    options.AddRetry("DefaultRetry", retryCount: 3, intervalMs: 500);
    options.AddCircuitBreaker("DefaultCircuitBreaker", 0.5, 10, 30);
    options.AddTimeout("DefaultTimeout", 5);
    options.AddBulkhead("DefaultBulkhead", 10, 20);
});

builder.Services
    .AddFranzPollyRetry("DefaultRetry")
    .AddFranzPollyCircuitBreaker("DefaultCircuitBreaker")
    .AddFranzPollyTimeout("DefaultTimeout")
    .AddFranzPollyBulkhead("DefaultBulkhead");
```

---

## 🧠 Observability Enhancements (v1.6.14)

Version **1.6.14** introduces a **resilience-awareness layer** across all Mediator pipelines.

### 🧩 `ResilienceContext`

Carries runtime state between pipelines:

```csharp
public sealed class ResilienceContext
{
    public string PolicyName { get; init; } = string.Empty;
    public int RetryCount { get; set; }
    public bool CircuitOpen { get; set; }
    public bool TimeoutOccurred { get; set; }
    public bool BulkheadRejected { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    public bool IsHealthy => !CircuitOpen && !TimeoutOccurred && !BulkheadRejected;
}
```

Each pipeline updates this context and emits structured logs through Serilog.

---

### 👁 `IResilienceObserver`

Observers can listen to policy outcomes globally:

```csharp
public interface IResilienceObserver
{
    void OnPolicyExecuted(string policyName, ResilienceContext context);
}
```

You can implement custom observers for metrics or telemetry (e.g., Application Insights, Prometheus, Elastic APM).

Example:

```csharp
public sealed class ElasticResilienceObserver : IResilienceObserver
{
    private readonly ILogger<ElasticResilienceObserver> _logger;

    public ElasticResilienceObserver(ILogger<ElasticResilienceObserver> logger)
        => _logger = logger;

    public void OnPolicyExecuted(string policyName, ResilienceContext context)
        => _logger.LogInformation("🧠 {Policy} -> Healthy={Healthy} Duration={Duration}ms Retries={RetryCount}",
            policyName, context.IsHealthy, context.Duration.TotalMilliseconds, context.RetryCount);
}
```

Register it once:

```csharp
builder.Services.AddSingleton<IResilienceObserver, ElasticResilienceObserver>();
```

---

## 📊 Pipelines Overview

| Pipeline                 | Options Class                        | Key                        | Observes Context |
| ------------------------ | ------------------------------------ | -------------------------- | ---------------- |
| Retry                    | `PollyRetryPipelineOptions`          | `"RetryPolicy"`            | ✅                |
| Circuit Breaker          | `PollyCircuitBreakerPipelineOptions` | `"CircuitBreaker"`         | ✅                |
| Advanced Circuit Breaker | `PollyAdvancedCircuitBreakerOptions` | `"AdvancedCircuitBreaker"` | ✅                |
| Timeout                  | `PollyTimeoutPipelineOptions`        | `"TimeoutPolicy"`          | ✅                |
| Bulkhead                 | `PollyBulkheadPipelineOptions`       | `"BulkheadPolicy"`         | ✅                |

All pipelines automatically participate in **Franz’s logging & correlation system**.

---

## 🧩 Example Logs (v1.6.14)

### Success

```plaintext
[12:01:22 INF] ▶️ Executing GetBookQuery [abc123] with RetryPolicy
[12:01:22 INF] ✅ GetBookQuery [abc123] succeeded after 47ms (policy RetryPolicy, retries=0)
```

### Retry + Timeout

```plaintext
[12:01:25 WRN] 🔁 GetBookQuery [abc123] retry attempt 2 (policy RetryPolicy)
[12:01:25 ERR] ⏱️ GetBookQuery [abc123] timed out after 5s (policy TimeoutPolicy)
```

### Circuit Breaker Open

```plaintext
[12:01:27 ERR] ❌ GetBookQuery [abc123] failed after 3 retries (policy RetryPolicy)
[12:01:27 WRN] 🚦 Circuit opened for 30s (policy CircuitBreaker)
```

---

## 🛠 Benefits

* 🧩 **Composability-first** — pipelines remain orthogonal yet share context.
* 🧠 **Self-aware architecture** — logs know what policies were triggered.
* 📈 **Observer hooks** — tap into resilience events for monitoring or dashboards.
* ⚡ **Zero boilerplate** — configured in <20 lines.
* 🏢 **Enterprise-ready** — deterministic, auditable, and DI-safe.

---

## 🗺 Roadmap

* [ ] `FranzResilienceSummaryPipeline` — emits aggregated resilience telemetry per request.
* [ ] OpenTelemetry integration via Activity tags.
* [ ] Prebuilt “DefaultSets” (HTTP, Database, Messaging).

---

## 📜 Changelog

### v1.6.14

* 🧠 Introduced `ResilienceContext` — unified runtime state for all pipelines.
* 👁 Added `IResilienceObserver` for external resilience monitoring.
* 🧾 Upgraded all pipelines to emit context-rich Serilog logs.
* 🔗 Added correlation ID propagation across all resilience policies.
* 🚀 Internal optimizations to reduce policy lookup overhead.

### v1.6.2

* ✨ Added `AddFranzResilience(IConfiguration)` for **config-driven resilience**.
* ♻️ Unified policy registry and Mediator pipelines.
* 🛡 Simplified startup — <20 lines bootstraps resilience + mediator.

---

⚡ With `Franz.Common.Mediator.Polly`, resilience is **first-class, observable, and deterministic**.
Configure it once — and your Mediator pipelines automatically enforce retries, timeouts, bulkheads, and breakers **with total visibility**.

---
