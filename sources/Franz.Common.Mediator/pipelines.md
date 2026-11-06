# Mediator pipeline composition

This guide describes the pipeline registration options exposed by `Franz.Common.Mediator`. For a complete request/handler example, see [example.md](example.md). For the full public contract and AOT matrix, see the [package README](readme.md).

## Pipeline contracts

Mediator separates request, notification, and event execution:

| Concern | Contract |
|---|---|
| Request/response behavior | `IPipeline<TRequest, TResponse>` |
| Request pre-processing | `IPreProcessor<TRequest>` |
| Request post-processing | `IPostProcessor<TRequest, TResponse>` |
| Notification behavior | `INotificationPipeline<TNotification>` |
| Event behavior | `IEventPipeline<TEvent>` |
| Event pre-processing | `IEventPreProcessor<TEvent>` |
| Event post-processing | `IEventPostProcessor<TEvent>` |

Do not assume that a request pipeline also wraps event or notification dispatch. Register the corresponding pipeline contract for the message type being handled.

## Built-in registration extensions

The core package exposes these opt-in registration methods:

| Extension | Registration |
|---|---|
| `AddFranzLoggingPipeline()` | Request and notification logging, plus logging pre/post-processors |
| `AddFranzValidationPipeline()` | Request and notification validation, plus audit pre-processing |
| `AddFranzBulkheadPipeline()` | Bulkhead request pipeline |
| `AddFranzCircuitBreakerPipeline()` | Circuit-breaker request pipeline |
| `AddFranzRetryPipeline()` | Retry request pipeline |
| `AddFranzTimeoutPipeline()` | Timeout request pipeline |
| `AddFranzEventValidationPipeline()` | Event validation/logging pipelines and event audit/logging processors |
| `AddFranzTransactionPipeline(configure)` | Transaction request pipeline and its `TransactionOptions` |
| `AddFranzSerilogLoggingPipeline()` | Serilog request/notification logging and processors |
| `AddFranzSerilogAuditPipeline()` | Validation pipelines and Serilog audit processors |

Register only the modules the application needs. Resilience modules are not automatically enabled by the standard or default helpers described below. Configure their options using the option types exposed by the corresponding module; do not copy configuration keys from older examples without checking the current package contract.

## Registration profiles and AOT

Choose the registration profile based on the required behavior and deployment model.

### Generated closed-generic default pipelines

```csharp
using Franz.Common.Mediator.Extensions;

services.AddFranzMediatorDefault<OrdersApplicationAssembly>();
```

This registers generated handlers from the marker assembly and applies generator-emitted closed-generic request and notification pipelines. It is the AOT-oriented default-pipeline composition. It includes request logging, validation, Serilog logging, transaction behavior, built-in logging/audit processors, and notification logging/validation. It does not include event validation or resilience pipelines.

### V2 default pipelines

```csharp
using Franz.Common.Mediator.Extensions;

services.AddFranzMediatorV2Default(
    handlerAssembly: typeof(OrdersApplicationAssembly).Assembly);
```

This applies open-generic request logging, validation, Serilog logging, and transaction pipelines, with matching request processors. Notifications receive logging and validation. This composition is not Native AOT compatible because it relies on open-generic pipeline registrations.

### Full standard composition

```csharp
using Franz.Common.Mediator.Bootstrap;

services.AddFranzMediatorStandard(
    new[] { typeof(OrdersApplicationAssembly).Assembly });
```

The standard helper is provided by the Mediator bootstrap package. It composes generated handler registration with request logging, validation, event validation, Serilog logging/audit, and transaction pipelines. It uses open-generic pipeline registrations and is not the Native AOT path. It does not automatically add retry, circuit-breaker, bulkhead, or timeout behavior.

### Explicit composition

For an application that needs a tailored set of behaviors, register core Mediator infrastructure and generated handlers, then add the required modules:

```csharp
using Franz.Common.Mediator.Extensions;

services.AddFranzMediator(
    new[] { typeof(OrdersApplicationAssembly).Assembly });

services
    .AddFranzLoggingPipeline()
    .AddFranzValidationPipeline()
    .AddFranzTransactionPipeline();
```

Add event validation and resilience explicitly when required. Confirm that each selected module's dependencies and options are registered by the application or its integration package.

## Execution semantics

Pipeline continuations are repeatable: each call to `next()` executes the downstream pipeline and handler chain again. This enables retry-style middleware, but it also means that a retry can repeat side effects.

Only retry operations whose side effects are safe to repeat, or protect them with idempotency and transaction/outbox strategies appropriate to the application. A transaction pipeline does not make an external side effect atomic with a database commit.

Pipeline registration order is an application composition detail. Verify the effective order with an integration test when behavior depends on nesting; do not infer transactional or retry ordering from the order of extension calls alone.

## Operational guidance

- Propagate the supplied `CancellationToken` through handlers and downstream I/O.
- Set bounded retry counts, delays, timeouts, and concurrency limits based on service-level objectives.
- Avoid retrying validation failures, permanent business errors, or non-idempotent side effects.
- Keep transaction boundaries around database work; do not hold database transactions open across slow remote calls.
- Test failure, cancellation, retry, timeout, and concurrency behavior against the actual pipeline composition.
- Use the generated closed-generic default composition when Native AOT is required. The V2-default and standard open-generic compositions are not AOT-compatible.

