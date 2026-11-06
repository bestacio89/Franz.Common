# Franz.Common.Messaging.MultiTenancy

> Validated tenant and domain execution scopes for broker-delivered messages.

## Overview

The messaging integration extracts tenant/domain identifiers from message headers, resolves an active tenant, optionally validates it, authorizes the authenticated producer, and establishes a scoped `TenantExecutionContext` for the message handler.

## Installation

```bash
dotnet add package Franz.Common.Messaging.MultiTenancy
```

## Registration

```csharp
using Franz.Common.Messaging.MultiTenancy.Extensions;

builder.Services.AddMessagingMultitenancyContext(options =>
{
    options.RequireTenantScope = true;
});

// Application-owned registrations:
builder.Services.AddScoped<ITenantStore, TenantStore>();
builder.Services.AddScoped<ITenantMessageAuthorizer, BrokerIdentityTenantMessageAuthorizer>();
```

The parameterless overload is also available. `RequireTenantScope` defaults to false because the package can be used for outbound metadata propagation without requiring every consumer to establish a tenant context.

When enabled, hosted messaging consumers validate that `ITenantMessageContextInitializer` is registered before they start listening. The initializer requires an `ITenantStore`, `ITenantExecutionContextAccessor`, and `ITenantMessageAuthorizer`. An optional `ITenantValidator` is also invoked when registered.

## Message contract

A tenant-scoped message must contain exactly one valid, non-empty GUID value in the standard tenant header. A domain header is optional; when present it must contain exactly one valid GUID and resolve to a verified domain associated with the tenant.

The initializer rejects unknown or inactive tenants, invalid domain ownership, failed tenant validation, and unauthorized producers. The authorizer must use trusted broker/transport identity; a message header alone is not proof that the producer may publish for that tenant.

## Execution lifecycle

The initializer exposes `BeginAsync(message, cancellationToken)`, which returns an async-disposable execution scope. Keep that scope alive for the full message handler execution and dispose it afterward. Disposal clears the scoped tenant execution context.

The package also registers tenant/domain message builders so outgoing messages can propagate the current validated context. Do not reuse a DI scope or tenant accessor across separate deliveries.

## Related packages

- `Franz.Common.MultiTenancy`: tenant contracts and execution context.
- `Franz.Common.Messaging`: message and header contracts.
- `Franz.Common.Messaging.Hosting.Kafka` and `Franz.Common.Messaging.Hosting.RabbitMQ`: hosted transport integration.

## License

MIT. See the repository [LICENSE](../../LICENSE).
