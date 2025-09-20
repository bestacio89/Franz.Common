Perfect 💯 Let’s make this README **full, polished, and Franz-style** with policy registration + pipeline mapping, logs, and benefits all connected.

Here’s the full file:

---

# **Franz.Common.Mediator.Polly**

**Franz.Common.Mediator.Polly** extends [Franz.Common.Mediator](https://www.nuget.org/packages/Franz.Common.Mediator/) with **Polly-based resilience pipelines**.
It gives you **retry, circuit breaker, timeout, bulkhead isolation, and advanced circuit breaker** for Mediator requests — all with **enriched Serilog logging built-in**.

No extra wiring. No extra boilerplate. Just plug and go.

---

* **Current Version**: 1.4.0

---

## ✨ Features

* 🔄 **Retry Pipeline**: automatic retries with backoff.
* ⚡ **Circuit Breaker Pipeline**: stop flooding failing dependencies.
* 🧠 **Advanced Circuit Breaker Pipeline**: trip based on failure rate in a rolling window.
* ⏱️ **Timeout Pipeline**: abort long-running requests.
* 🚧 **Bulkhead Pipeline**: limit concurrent executions.
* 📝 **Enriched Logging**: every pipeline execution logs:

  * Correlation ID
  * Request type
  * Policy name
  * Pipeline name
  * Success/failure status
  * Execution time

Logs are pushed into **Serilog** with all the contextual properties you already get from `SerilogLoggingPipeline`.

---

## 📦 Installation

```bash
dotnet add package Franz.Common.Mediator.Polly
```

---

## ⚙️ Setup

### 1. Centralized Policy Registration

Instead of scattering `PolicyRegistry.Add(...)` calls, use `AddFranzPollyPolicies()` to declare everything in one place:

```csharp
using Polly;
using Franz.Common.Mediator.Polly.Extensions;

builder.Services.AddFranzPollyPolicies(options =>
{
    // 🔁 Retry policy with exponential backoff
    options.Policies["DefaultRetry"] = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
        );

    // 🚦 Circuit breaker (basic)
    options.Policies["DefaultCircuitBreaker"] = Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 2,
            durationOfBreak: TimeSpan.FromSeconds(30)
        );

    // 🧠 Advanced circuit breaker (failure rate over time)
    options.Policies["DefaultAdvancedBreaker"] = Policy
        .Handle<Exception>()
        .AdvancedCircuitBreakerAsync(
            failureThreshold: 0.5,                // trip if 50% fail
            samplingDuration: TimeSpan.FromSeconds(30),
            minimumThroughput: 8,                 // at least 8 calls in window
            durationOfBreak: TimeSpan.FromSeconds(15)
        );

    // ⏱ Timeout policy
    options.Policies["DefaultTimeout"] = Policy
        .TimeoutAsync(TimeSpan.FromSeconds(5));

    // 🚧 Bulkhead isolation
    options.Policies["DefaultBulkhead"] = Policy
        .BulkheadAsync(
            maxParallelization: 10,
            maxQueuingActions: 20
        );
});
```

---

### 2. Register Mediator + Pipelines

Now wire policies into pipelines by name — the Franz way:

```csharp
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Polly.Extensions;

builder.Services
    .AddFranzMediator(typeof(Program).Assembly)
    .AddFranzRetryPipeline("DefaultRetry")
    .AddFranzCircuitBreakerPipeline("DefaultCircuitBreaker")
    .AddFranzAdvancedCircuitBreakerPipeline("DefaultAdvancedBreaker")
    .AddFranzTimeoutPipeline("DefaultTimeout")
    .AddFranzBulkheadPipeline("DefaultBulkhead");
```

That’s it. One registry + one mapping = **full resilience layer** with enriched logging.

---

### 3. Example Logs (Serilog)

When a request runs successfully:

```plaintext
▶️ Handling GetBookQuery [correlationId=abc123] with policy DefaultRetry
✅ GetBookQuery [correlationId=abc123] completed in 54ms (policy DefaultRetry)
```

If retries fail and the circuit breaker opens:

```plaintext
❌ GetBookQuery [correlationId=abc123] failed after 3 retries (policy DefaultRetry)
⚡ Circuit opened for 30s (policy DefaultCircuitBreaker)
```

If a timeout is triggered:

```plaintext
⏱️ GetBookQuery [correlationId=abc123] timed out after 5s (policy DefaultTimeout)
```

---

## 🧩 Pipelines Overview

| Pipeline                | Options Class                                | Example Policy Key         |
| ----------------------- | -------------------------------------------- | -------------------------- |
| Retry                   | `PollyRetryPipelineOptions`                  | `"DefaultRetry"`           |
| Circuit Breaker         | `PollyCircuitBreakerPipelineOptions`         | `"DefaultCircuitBreaker"`  |
| Advanced CircuitBreaker | `PollyAdvancedCircuitBreakerPipelineOptions` | `"DefaultAdvancedBreaker"` |
| Timeout                 | `PollyTimeoutPipelineOptions`                | `"DefaultTimeout"`         |
| Bulkhead                | `PollyBulkheadPipelineOptions`               | `"DefaultBulkhead"`        |

Each pipeline resolves its named policy from the `IReadOnlyPolicyRegistry<string>` you configure.

---

## ✅ Benefits

* **Centralized** resilience policy registration.
* **Opt-in**: only register the pipelines you need.
* **Zero boilerplate**: enriched logging comes out-of-the-box.
* **Structured observability**: Serilog correlation IDs across retries, timeouts, and circuit breakers.
* **Enterprise-ready**: clean DI, config-driven, consistent patterns.

---

## 🔮 Roadmap

* [ ] Policy composition helpers (chaining retry + circuit breaker).
* [ ] Prebuilt default policy sets for common scenarios.

---

👉 With `Franz.Common.Mediator.Polly`, using Polly inside Mediator is **frictionless**: resilience + structured logs, with just two extension calls.

---


