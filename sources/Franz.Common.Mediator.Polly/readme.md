# Franz.Common.Mediator.Polly

> Polly policy registration for HTTP clients and Franz Mediator request pipelines.

## Overview

The package exposes two distinct registration methods. They do not have the same behavior. HTTP policies are placed in a policy registry; the package does not automatically attach them to arbitrary HttpClient or Refit registrations.

## Installation

```bash
dotnet add package Franz.Common.Mediator.Polly
```

## HTTP policy registry only

```csharp
builder.Services.AddFranzResilience(builder.Configuration);
```

`AddFranzResilience(configuration)` reads the `Resilience` section and registers an `IReadOnlyPolicyRegistry<string>` containing enabled HTTP response policies for retry, advanced circuit breaker, timeout, and bulkhead. It does **not** register a Mediator pipeline.

## Mediator and HTTP resilience

```csharp
builder.Services.AddFranzResiliencev2(builder.Configuration);
```

`AddFranzResiliencev2(configuration)` registers enabled base Mediator policies and HTTP policies, composes optional named profiles, registers the policy registry, and adds `PollyResiliencePipeline<TRequest, TResponse>` to Mediator.

The profile sections are `Resilience:MediatorProfiles` and `Resilience:HttpProfiles`. Each profile contains a `Policies` array referencing the corresponding registered policy names (for example, `mediator:Retry` or `http:Retry`).

## Configuration

```json
{
  "Resilience": {
    "RetryPolicy": {
      "Enabled": true,
      "RetryCount": 3,
      "RetryIntervalMilliseconds": 200
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
    },
    "MediatorProfiles": {
      "Default": {
        "Policies": [
          "mediator:Retry",
          "mediator:CircuitBreaker",
          "mediator:Timeout"
        ]
      }
    },
    "HttpProfiles": {
      "Default": {
        "Policies": [
          "http:Retry",
          "http:CircuitBreaker",
          "http:Timeout"
        ]
      }
    }
  }
}
```

The base policy keys are shared configuration switches; enabling a policy registers the corresponding policy instances. Profiles compose those registered instances. Validate retry safety for each operation: automatic retries are appropriate only when the operation is idempotent or protected by an idempotency mechanism.

## API naming

Use the current public methods `AddFranzResilience` for HTTP registry-only setup and `AddFranzResiliencev2` for Mediator plus HTTP setup. Historical examples using `AddFranzPollyPolicies`, `AddFranzPollyRetry`, or separate per-policy registration methods do not describe the current package API.

## License

MIT. See the repository [LICENSE](../../LICENSE).
