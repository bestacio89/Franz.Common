# Franz.Common.Messaging.Hosting

> Generic host integration for Franz message listeners and message execution scopes.

## Overview

This package contains the generic `MessagingHostedService`, the `UseMessaging` host-builder extensions, message-context handling, and the outbox listener implementation. Transport-specific hosted services should generally be registered through their corresponding hosting packages.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Hosting
```

## Host composition

The package exposes two `UseMessaging` overloads:

```csharp
hostBuilder.UseMessaging(
    configure: (services, configuration) =>
    {
        // Register the selected transport and application handlers.
    },
    assembly: typeof(Program).Assembly);
```

The generic Startup overload expects a startup type with a constructor accepting `IHostEnvironment` and `IConfiguration`, plus a `ConfigureServices(IServiceCollection)` method:

```csharp
hostBuilder.UseMessaging<MessagingStartup>(typeof(Program).Assembly);
```

Both overloads scan the application base directory for DLL names matching the entry assembly's product prefix, load matching assemblies that are not already loaded, configure logging, and register `MessagingHostedService`.

## Listener lifecycle constraint

`MessagingHostedService.StartAsync` awaits `IListener.Listen(cancellationToken)`. Therefore, the listener must return after its startup/registration phase. A listener whose `Listen` method runs a polling or consumption loop until cancellation will block generic-host startup when used with this service.

For the supported Kafka and RabbitMQ background-consumer lifecycles, prefer `Franz.Common.Messaging.Hosting.Kafka` or `Franz.Common.Messaging.Hosting.RabbitMQ`; those packages use `BackgroundService` and keep the long-running listener attached to the host's execution lifecycle.

## Message execution

The hosted service creates a DI scope per delivered message, establishes message context, optionally initializes validated tenant execution context, resolves an `IMessagingStrategyExecuter`, and executes the selected strategy. The scope is disposed after processing.

## License

MIT. See the repository [LICENSE](../../LICENSE).
