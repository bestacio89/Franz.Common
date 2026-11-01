# Franz.Common.Messaging.AzureEventHubs

Azure Event Hubs integration for Franz.Common.

## Install

```bash
dotnet add package Franz.Common.Messaging.AzureEventHubs --version 2.3.4
```

## Processing model

The Event Hubs processor deserializes and maps each event to a Franz message before dispatching it. When the scope factory and messaging multi-tenancy integration are registered, each event is processed in a fresh async DI scope. The tenant initializer validates tenant/domain context before the scoped dispatcher is invoked. The event checkpoint is updated only after successful dispatch.

Processing failures propagate from the event handler; configure EventProcessorClient retry, partition ownership and checkpoint storage behaviour for deployment. Monitor poison events and repeated failures.

## Tenant-aware requirements

Register AddMessagingMultitenancyContext(), ITenantStore and ITenantMessageAuthorizer. Broker metadata is untrusted input; the initializer resolves active tenant state, applies optional validation, authorizes the producer and validates domain ownership before dispatch.

Ensure the processor receives IServiceScopeFactory when tenant-aware execution is required.

Framework release line: **2.3.4**; target framework: net10.0.

See [messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md).

MIT License.
