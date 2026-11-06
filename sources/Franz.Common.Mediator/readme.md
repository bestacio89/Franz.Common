# Franz.Common.Mediator

> Mediator dispatch for commands, queries, notifications, and pipeline behaviors with explicit registration and AOT-aware execution.

## Overview

Mediator dispatch for commands, queries, notifications, and pipeline behaviors with explicit registration and AOT-aware execution.

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

This registers the mediator infrastructure and generated handlers from the marker type's assembly, then applies generator-emitted closed-generic pipelines. Commands, queries, and void commands receive logging, validation, Serilog logging, and transaction pipelines plus the built-in logging/audit pre- and post-processors. Notifications receive notification logging and notification validation. It does not add event-validation or resilience pipelines. The generated closed-generic pipeline contribution is the AOT-oriented default-pipeline path.

### V2 registration API

The V2 entry point remains available for compatibility and explicit use:

```csharp
using Franz.Common.Mediator.Extensions;

services.AddFranzMediatorV2(
    handlerAssembly: typeof(CreateHeroHandler).Assembly);
```

It uses the same compile-time generated handler-registration model. `AddFranzMediatorV2Default` adds open-generic request logging, validation, Serilog logging, and transaction pipelines, plus the matching request pre/post-processors; notifications receive notification logging and validation. Those open-generic pipeline registrations are not supported by Native AOT, and the helper does not add event validation. V1 and V2 core registration are API-level entry points; neither falls back to runtime reflection-based handler discovery.

#### Full standard composition

For the complete standard pipeline composition, including event validation, use the bootstrap helper:

```csharp
using Franz.Common.Mediator.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFranzMediatorStandard(
    new[] { typeof(CreateHeroHandler).Assembly });
```

This registers the V1 generated handler contribution plus logging, validation, event validation, Serilog logging/audit, and transaction pipelines. It uses open-generic pipeline registrations and is not the Native AOT path. Add retry, circuit-breaker, bulkhead, and timeout pipelines explicitly when required; they are not part of the standard helper.

## Core infrastructure only

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

For discovered command/query/void-command contracts, generated code emits typed invokers that resolve registered pre-processors and post-processors from DI; application-defined processors must still be registered with the container. The generator also discovers and registers supported `IValidator<T>` implementations. Stream-query handlers are registered for direct DI resolution rather than through the command/query invoker registry.

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
var streamDispatcher = (FranzDispatcher)dispatcher;

await foreach (var item in streamDispatcher.Stream<GetHeroesQuery, HeroDto>(
    new GetHeroesQuery(),
    cancellationToken))
{
    // Process each result.
}
```

## Generated runtime architecture

At build time, the source generator inspects the selected compilation and emits:

- assembly-scoped handler registration contributions;
- strongly typed command, void-command, query, event, and request-processor invokers;
- generated invoker registry registrations used by dispatch;
- direct DI registrations for stream-query handlers and notification handlers;
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

`AddFranzMediatorDefault<TAssemblyMarker>` applies generated closed-generic request/notification pipelines and is the AOT-oriented default composition; it does not include event validation or resilience. `AddFranzMediatorV2Default` and `AddFranzMediatorStandard` use open-generic pipelines and are not Native AOT compositions. The standard helper includes event validation; the V2 default helper does not. Register event validation and resilience explicitly when using the core registration APIs.

## AOT and trimming

The generated registration and invoker path is designed for trimming and Native AOT:

- handler discovery is performed by the source generator;
- generated invokers use compile-time-known handler and contract types;
- runtime expression compilation and reflective handler/processor invocation are not required;
- the source generator is packed with the Mediator NuGet package.

The repository's Native AOT smoke application publishes and executes mapping, caching, and Mediator scenarios. The V1 Mediator path exercises commands, queries, void commands, pre/post-processors, notifications, events, stream queries, and the generated closed-generic default pipelines; the V2 core registration path is exercised with a query. The smoke coverage does not make the open-generic `AddFranzMediatorV2Default` or `AddFranzMediatorStandard` pipeline compositions AOT-safe. This is an executable gate for the exercised paths, not a substitute for application-specific integration tests.

## Compatibility

The existing V1 and V2 public registration entry points are retained. The legacy `ReflectionHandlerRegistrationProvider` remains available but is not used by the generated registration APIs. `HandlerCollector` is still used by the Business registration helper and performs runtime collection; it is not the Native AOT registration path. The supported Mediator registration flow applies generated contributions rather than performing runtime handler discovery.

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


## License

Licensed under the MIT License. See the repository [LICENSE](../../LICENSE) for details.
