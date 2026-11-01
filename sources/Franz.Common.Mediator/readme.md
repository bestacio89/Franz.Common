# Franz.Common.Mediator

> A compile-time-generated application dispatch and pipeline runtime for Franz.Common on .NET 10.

## Overview

`Franz.Common.Mediator` provides strongly typed dispatch for application commands, queries, notifications, events, and stream queries. Handler discovery and invoker generation happen at compile time through the Roslyn source generator shipped with the Mediator package.

The runtime uses generated registration contributions and strongly typed invokers instead of runtime assembly scanning, expression-tree compilation, or reflective handler/processor invocation. This keeps application composition deterministic and supports trimming and Native AOT.

## Installation

```bash
dotnet add package Franz.Common.Mediator
```

The package includes the Mediator source generator under its analyzer assets. Application projects should reference `Franz.Common.Mediator`; they do not need to install or reference the generator separately.

## Registration

### Recommended registration

Select the application assembly (or assemblies) that contain the handlers. The generator emits registration contributions for those assemblies, and the registration API applies them to the service collection.

```csharp
using Franz.Common.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddFranzMediator(
    new[]
    {
        typeof(CreateHeroHandler).Assembly
    });
```

The assembly selection is explicit. The generated contribution is associated with the selected assembly, so applications can compose handlers from multiple modules without relying on an implicit application-wide scan.

### Default pipeline setup

Use the marker-type overload when you also want the default enterprise pipeline registrations:

```csharp
services.AddFranzMediatorDefault<ApplicationAssemblyMarker>();
```

This registers the mediator infrastructure and generated handlers from the marker type's assembly, followed by the default logging, validation, Serilog logging/audit, and transaction pipelines.

### V2 registration API

The V2 entry point remains available for compatibility and explicit use:

```csharp
services.AddFranzMediatorV2(
    handlerAssembly: typeof(CreateHeroHandler).Assembly);
```

It uses the same compile-time generated registration model. Its default-pipeline counterpart is `AddFranzMediatorV2Default`. V1 and V2 are API-level registration entry points; neither falls back to runtime reflection-based handler discovery.

### Core infrastructure only

The parameterless overload registers the dispatcher and core Mediator infrastructure without selecting an application handler assembly:

```csharp
services.AddFranzMediator();
```

Use this overload when handler registration is deliberately composed separately. To register generated handlers independently, use `AddFranzGeneratedHandlerRegistration` with the target assembly.

## Supported handler contracts

The generated registration and invoker model covers:

- `ICommandHandler<TCommand, TResult>`
- `ICommandHandler<TCommand>` (void commands)
- `IQueryHandler<TQuery, TResult>`
- `INotificationHandler<TNotification>`
- `IEventHandler<TEvent>`
- `IStreamQueryHandler<TQuery, TResult>`

The generator also discovers and generates strongly typed invokers for command/query pre-processors and post-processors, and supported validation handlers.

Keep each handler focused on its contract. Handler implementations are resolved through Microsoft.Extensions.DependencyInjection using the registered service lifetimes.

## Dispatch

Resolve `IDispatcher` from the application scope and dispatch through its typed APIs:

```csharp
var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

var hero = await dispatcher.SendAsync<HeroDto>(
    new GetHeroQuery(heroId),
    cancellationToken);

await dispatcher.SendAsync(
    new DeleteHeroCommand(heroId),
    cancellationToken);

await dispatcher.PublishNotificationAsync(
    new HeroCreatedNotification(heroId),
    cancellationToken);

await dispatcher.PublishEventAsync(
    new HeroCreatedEvent(heroId),
    cancellationToken);
```

Stream queries are exposed by the dispatcher implementation through asynchronous enumeration:

```csharp
await foreach (var item in dispatcher.Stream<GetHeroesQuery, HeroDto>(
    new GetHeroesQuery(),
    cancellationToken))
{
    // Process each result.
}
```

## Generated runtime architecture

At build time, the source generator inspects the selected compilation and emits:

- assembly-scoped handler registration contributions;
- strongly typed command, void-command, query, event, and processor invokers;
- generated invoker registry registrations used by dispatch;
- valid contributions for assemblies that contain no supported handlers.

At runtime, the selected generated contribution is applied to the service collection. Dispatch uses typed delegates and dependency-injection resolution. The Mediator runtime does not need to scan handler assemblies, compile expression trees, construct generic methods reflectively, or invoke handlers/processors through reflection.

The generator is included in the NuGet package at:

```text
analyzers/dotnet/cs/Franz.Common.Mediator.SourceGenerator.dll
```

A normal package reference is sufficient to activate it in a consuming project.

## Pipeline execution

Mediator pipelines provide composable cross-cutting behavior around request handling.

### Commands and queries

The request execution model supports:

- `IPipeline<TRequest, TResponse>`
- `IPreProcessor<TRequest>`
- handler execution
- `IPostProcessor<TRequest, TResponse>`
- mediator observers

### Events and notifications

Events and notifications retain their distinct dispatch contracts and pipeline models. Event processing supports event pipelines, pre-processors, post-processors, and event handlers. Notifications use notification handlers and notification pipelines.

### Repeatable continuation

Pipeline continuations are repeatable. If middleware invokes `next()` more than once, each invocation executes the downstream pipeline and handler chain again. This is required for retry-style middleware and is covered by generic and void pipeline executor tests.

## Built-in pipeline modules

Register only the modules required by the application:

- **Logging:** request and notification logging.
- **Validation and auditing:** validation pipelines and audit processors.
- **Resilience:** retry, circuit breaker, bulkhead isolation, and timeout pipelines.
- **Transactions:** transaction boundary support.
- **Serilog integration:** structured logging and audit enrichment.
- **Event validation:** event-specific validation, logging, and audit processing.

The `AddFranzMediatorDefault<TAssemblyMarker>` and `AddFranzMediatorV2Default` methods provide the documented default combination. Individual pipeline registration extensions are available when an application needs a more specific composition.

## AOT and trimming

The generated registration and invoker path is designed for trimming and Native AOT:

- handler discovery is performed by the source generator;
- generated invokers use compile-time-known handler and contract types;
- runtime expression compilation and reflective handler/processor invocation are not required;
- the source generator is packed with the Mediator NuGet package.

The repository's Native AOT smoke application publishes and executes mapper and Mediator scenarios, including V1 and V2 registration, commands, queries, void commands, pre/post-processors, notifications, events, and stream queries. This provides an executable AOT compatibility gate for the exercised paths; it is not a substitute for application-specific integration tests.

## Compatibility

The existing V1 and V2 public registration entry points are retained. Legacy public reflection-provider and handler-collector types remain as compatibility shims, but the supported registration flow applies generated contributions rather than performing runtime handler discovery.

For new applications, use explicit assembly selection with `AddFranzMediator(assemblies)` or `AddFranzMediatorV2(handlerAssembly: ...)`.

## Architectural boundaries

Franz Mediator:

**Does**
- dispatch in-process application requests and messages;
- compose request, notification, and event pipelines;
- resolve handlers and processors through dependency injection;
- provide generated, strongly typed registration and invocation;
- support deterministic, trimming-aware, Native AOT-oriented composition.

**Does not**
- replace message brokers or transport systems;
- consume distributed messages from Kafka, RabbitMQ, or other brokers;
- own hosted-service orchestration or distributed communication.

## Architectural direction

Mediator's execution model remains explicit, composable, and contract-oriented. The current implementation moves handler composition and invoker construction to compile time, removing runtime discovery and reflective dispatch from the supported generated path while preserving the established registration APIs.
