# Franz.Common.Messaging.AzureEventBus

Azure Service Bus integration for Franz.Common.

## Install

```bash
dotnet add package Franz.Common.Messaging.AzureEventBus --version 2.3.4
```

## Processing model

The processor maps Service Bus deliveries into Franz message envelopes and dispatches them through the mediator. When the service scope factory and messaging multi-tenancy integration are registered, each delivery gets a fresh async DI scope. Tenant metadata is validated through ITenantMessageContextInitializer before dispatch, and the scoped dispatcher is resolved from that delivery scope.

Successful dispatch completes the broker message. Processing failures are logged and the delivery is abandoned according to the processor failure path. Mapping failures are dead-lettered. Configure broker retry, lock duration, dead-letter monitoring and duplicate handling as part of deployment.

## Tenant-aware requirements

Register AddMessagingMultitenancyContext(), ITenantStore and ITenantMessageAuthorizer. Tenant/domain headers are untrusted metadata until resolved and authorized. Ensure the processor is composed with IServiceScopeFactory; without scoped tenant integration the processor retains its non-tenant dispatch path.

Framework release line: **2.3.4**; target framework: net10.0.

See [messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md).

MIT License.
