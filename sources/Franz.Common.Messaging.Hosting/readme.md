# Franz.Common.Messaging.Hosting

> Generic-host integration, per-delivery execution context, strategy execution, and message action-filter contracts.

## Overview

This package provides the generic MessagingHostedService, message context accessor, strategy executor contract, action-filter contracts, and the OutboxMessageListener. Broker-specific hosted services are provided by the Kafka, RabbitMQ, and Azure hosting packages.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.Hosting
~~~

## Host integration

The package exposes UseMessaging host-builder overloads in Franz.Common.Messaging.Hosting.Extensions:

- UseMessaging<TStartup>(assembly) creates the supplied Startup type using an IHostEnvironment and IConfiguration constructor, calls its ConfigureServices(IServiceCollection), and registers MessagingHostedService.
- UseMessaging(configure, assembly) registers MessagingHostedService and invokes the supplied service-configuration callback.

The host must register an IListener implementation and the IMessagingStrategyExecuter implementations required to process incoming messages. Transport-specific packages provide their own lifecycle integrations and should be preferred when available.

## Per-message execution

MessagingHostedService assigns the listener callback and processes each message in a dependency-injection scope. It establishes MessageContextAccessor state, optionally initializes a validated tenant execution context through ITenantMessageContextInitializer, selects the first IMessagingStrategyExecuter whose CanExecuteAsync returns true, executes it, and clears the message context.

Message handlers and strategy executors should be scoped and should observe cancellation. Tenant context must be established from trusted producer identity and validated tenant data; a tenant header alone is not authorization.

## Strategy contract

~~~csharp
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Messages;

public sealed class OrderMessageStrategy : IMessagingStrategyExecuter
{
    public Task<bool> CanExecuteAsync(Message message)
        => Task.FromResult(message.MessageType == typeof(OrderCreated).AssemblyQualifiedName);

    public Task ExecuteAsync(Message message)
    {
        // Resolve the application handler from the message execution scope
        // and dispatch the deserialized message.
        return Task.CompletedTask;
    }
}
~~~

The actual interface methods accept Franz.Common.Messaging.Messages.Message. They do not accept MessageContext as the ExecuteAsync argument.

## Message action filters

IAsyncMessageActionFilter is a messaging execution filter, not an ASP.NET MVC action filter. Its contract is OnActionExecutionAsync(MessageActionExecutingContext, MessageActionExecutionDelegate). Do not register TransactionFilter in MVC's ControllerOptions.Filters collection.

TransactionFilter calls CompleteAsync when the downstream action completes without an exception and RollbackAsync when it completes with an exception. Transaction creation/ownership must be supplied by the surrounding messaging execution path.

## Outbox listener

OutboxMessageListener implements IListener and polls IMessageStore for pending messages using the configured OutboxOptions batch size. Each message is processed in an async DI scope, checked against IInboxStore, optionally initialized with validated tenant context, deserialized, dispatched through IDispatcher, then marked processed and sent.

The listener requires IMessageStore, IInboxStore, IDispatcher, IMessageSerializer, and OutboxOptions. It polls at a fixed five-second interval. It does not guarantee exactly-once effects across the business handler and the inbox/outbox stores; make handlers idempotent.

## Lifecycle constraint

The current MessagingHostedService awaits IListener.Listen from IHostedService.StartAsync. A listener whose Listen method runs until host cancellation will therefore keep host startup blocked. The current OutboxMessageListener uses that long-running loop shape. Prefer the transport-specific hosted-service integrations for production consumers; the generic listener/host lifecycle needs a non-blocking startup contract before it can safely host a cancellation-bound loop.

## Related packages

- Franz.Common.Messaging — core message and persistence contracts.
- Franz.Common.Messaging.Hosting.Kafka — Kafka hosted listener and outbox execution.
- Franz.Common.Messaging.Hosting.RabbitMQ — RabbitMQ hosted listener and outbox execution.
- Franz.Common.Messaging.Hosting.Azure — Azure Service Bus/Event Hubs hosted execution.

## License

MIT. See the repository [LICENSE](../../LICENSE).
