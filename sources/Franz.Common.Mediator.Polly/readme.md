# **Franz.Common.Mediator.Polly**

**Franz.Common.Mediator.Polly** extends [Franz.Common.Mediator](https://www.nuget.org/packages/Franz.Common.Mediator/) with **Polly-based resilience pipelines**.
It gives you **retry, circuit breaker, advanced circuit breaker, timeout, and bulkhead isolation** for Mediator requests — all with **enriched Serilog logging built-in**.

> ⚡ No extra wiring. No boilerplate. Just plug it in.

---

* **Current Version**: 1.6.2
* **Target Frameworks**: .NET 9 +
* **Dependencies**: `Polly`, `Serilog`, `Franz.Common.Mediator`

---

## ✨ Features

* 🔁 **Retry Pipeline**: automatic retries with configurable backoff.
* 🚦 **Circuit Breaker Pipeline**: stop flooding failing dependencies.
* 📊 **Advanced Circuit Breaker Pipeline**: trip based on failure rate in a rolling window.
* ⏱ **Timeout Pipeline**: abort long-running requests.
* 📦 **Bulkhead Pipeline**: limit concurrent executions.
* 📝 **Enriched Logging**: every execution logs with:

  * Correlation ID
  * Request type
  * Policy name
  * Pipeline name
  * Success/failure status
  * Execution time

Logs integrate seamlessly into **Serilog** with the same structured properties as `SerilogLoggingPipeline`.

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

That’s it — retry, circuit breaker, timeout, and bulkhead are auto-registered from config and wired into Mediator pipelines.

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

### 3. Example Logs (Serilog)

Success:

```plaintext
[12:01:22 INF] Handling GetBookQuery [correlationId=abc123] with policy RetryPolicy
[12:01:22 INF] GetBookQuery [correlationId=abc123] completed in 54ms (policy RetryPolicy)
```

Retries exhausted + breaker open:

```plaintext
[12:01:23 ERR] GetBookQuery [correlationId=abc123] failed after 3 retries (policy RetryPolicy)
[12:01:23 WRN] Circuit opened for 30s (policy CircuitBreaker)
```

Timeout:

```plaintext
[12:01:25 ERR] GetBookQuery [correlationId=abc123] timed out after 5s (policy TimeoutPolicy)
```

---

## 📊 Pipelines Overview

| Pipeline                | Options Class                        | Policy Key Example         |
| ----------------------- | ------------------------------------ | -------------------------- |
| Retry                   | `PollyRetryPipelineOptions`          | `"RetryPolicy"`            |
| Circuit Breaker         | `PollyCircuitBreakerPipelineOptions` | `"CircuitBreaker"`         |
| Advanced CircuitBreaker | `PollyAdvancedCircuitBreakerOptions` | `"AdvancedCircuitBreaker"` |
| Timeout                 | `PollyTimeoutPipelineOptions`        | `"TimeoutPolicy"`          |
| Bulkhead                | `PollyBulkheadPipelineOptions`       | `"BulkheadPolicy"`         |

Each pipeline looks up its named policy from the `IReadOnlyPolicyRegistry<string>` registered in DI.

---

## 🚀 Benefits

* 🔗 **Config-driven** resilience (v1.6.2+).
* 🛠 **Centralized** policy registration.
* 🧩 **Composable**: opt-in only the pipelines you need.
* 📡 **Observability-first**: structured Serilog logs across retries, timeouts, bulkheads, and breakers.
* 🏢 **Enterprise-ready**: clean DI patterns, no boilerplate.

---

## 🗺 Roadmap

* [ ] Policy composition helpers (retry + breaker combos).
* [ ] Prebuilt default sets (`HttpDefault`, `DbDefault`, etc.).
* [ ] OpenTelemetry tags for resilience events.

---

## 📜 Changelog

### v1.6.2

* ✨ Added `AddFranzResilience(IConfiguration)` for **config-driven resilience**.
* ♻️ Unified policy registry + Mediator pipelines into a single entrypoint.
* 🛡 Simplified startup — <20 lines bootstraps resilience + mediator.
* 🔧 Requires `Microsoft.Extensions.Configuration.Binder`.

### v1.4.4

* ✅ Fixed policy registry to consistently use `IAsyncPolicy<HttpResponseMessage>`.
* ✅ Corrected mediator pipeline registrations for generics.

---

⚡ With `Franz.Common.Mediator.Polly`, resilience is **first-class and frictionless**: configure it once, and Mediator pipelines automatically enforce retries, timeouts, bulkheads, and breakers with structured logs.

