# Franz.Common.Mediator.Polly

> Polly policy-registry integration for HTTP resilience and Mediator request execution.

## Overview

This package provides two registration entry points with different responsibilities. They must not be described as interchangeable.

- AddFranzResilience registers configured HTTP policies in a Polly policy registry. It does not register a Mediator pipeline.
- AddFranzResiliencev2 registers both HTTP and Mediator policies, composes named profiles, and registers PollyResiliencePipeline<TRequest, TResponse> as a Mediator pipeline.

## Installation

~~~bash
dotnet add package Franz.Common.Mediator.Polly
~~~

## Mediator and HTTP resilience

~~~csharp
using Franz.Common.Mediator.Polly;

services.AddFranzResiliencev2(configuration);
~~~

The configuration is read from the Resilience section. Base policy switches and values are shared by the HTTP and Mediator policy registration paths:

~~~json
{
  "Resilience": {
    "RetryPolicy": {
      "Enabled": true,
      "RetryCount": 3,
      "RetryIntervalMilliseconds": 200
    },
    "TimeoutPolicy": {
      "Enabled": true,
      "TimeoutSeconds": 5
    },
    "CircuitBreaker": {
      "Enabled": true,
      "FailureThreshold": 0.5,
      "MinimumThroughput": 10,
      "DurationOfBreakSeconds": 30
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
          "mediator:Timeout",
          "mediator:CircuitBreaker",
          "mediator:Bulkhead"
        ]
      }
    }
  }
}
~~~

The profile name used by the Mediator pipeline defaults to Default. AddFranzResiliencev2 creates a registry entry named mediator:{profileName}; it does not synthesize a Default profile. Configure MediatorProfiles:Default explicitly and ensure its Policies entries reference registered policy keys. An unregistered profile or policy fails explicitly during composition or execution. HttpProfiles can define corresponding HTTP composites using the registered http:* policy keys.

The Mediator pipeline wraps the downstream handler execution in the selected composite policy. It preserves exceptions, records elapsed duration, adds request/correlation/profile properties to Serilog's LogContext, and notifies registered IResilienceObserver implementations after success or failure.

Register observers through dependency injection:

~~~csharp
services.AddSingleton<IResilienceObserver, ApplicationResilienceObserver>();
~~~

Observers are diagnostic callbacks; they should not perform blocking work or alter the request result.

## HTTP-only policy registration

Use AddFranzResilience when only the HTTP policy registry is required:

~~~csharp
services.AddFranzResilience(configuration);
~~~

It registers the HTTP policies in the policy registry. It does not add PollyResiliencePipeline<TRequest, TResponse> to the Mediator pipeline. Use the v2 entry point when Mediator resilience is required.

## Policy behavior

The available base policies are retry, timeout, advanced circuit breaker, and bulkhead isolation. HTTP policies handle HTTP request exceptions and unsuccessful HTTP responses, but this package only places them in the Polly registry; it does not automatically attach them to HttpClientFactory or a typed client. The application must apply the selected HTTP policy/profile through its HTTP client integration. Mediator policies handle exceptions from the downstream request pipeline.

Timeout uses Polly's optimistic cancellation model. The downstream operation must observe cancellation; synchronous or cancellation-ignoring work cannot be forcibly stopped.

Retry can execute the downstream operation more than once. Apply it only where repeated execution is safe or where the operation is idempotent. Do not apply retry indiscriminately to commands with non-idempotent side effects.

## Scope and limitations

This package uses Polly's policy-registry model. It does not claim that every policy applies to every execution path: the HTTP registry and Mediator pipeline are separate integrations. Configure and test each path that the application uses.

## License

MIT. See the repository [LICENSE](../../LICENSE).
