# Franz.Common.Messaging.MultiTenancy

> Validated tenant execution context and tenant/domain metadata for message delivery and publication.

## Overview

This package connects the provider-neutral tenant contracts to the messaging execution boundary. It provides scoped tenant/domain accessors, message builders, header/property resolvers, and a delivery context initializer.

A tenant ID in a message is an input to resolution, not proof that the producer is authorized for that tenant. Before tenant-scoped application code runs, the initializer loads the tenant, checks that it is active, invokes the optional application tenant validator, verifies the producer through ITenantMessageAuthorizer, and validates any supplied domain against the tenant's verified domains.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.MultiTenancy
~~~

## Registration

Register the messaging tenant integration through AddMessagingMultitenancyContext:

~~~csharp
using Franz.Common.Messaging.MultiTenancy.Extensions;

services.AddMessagingMultitenancyContext(options =>
{
    options.RequireTenantScope = true;
});
~~~

This registers scoped tenant/domain accessors, the scoped execution-context accessor, the tenant message context initializer, and the tenant/domain message builders.

The application must also register an ITenantStore and an ITenantMessageAuthorizer. Register an ITenantValidator when application-specific lifecycle validation is required. The authorizer must verify the trusted transport producer identity; it must not authorize solely from a tenant header supplied by the message.

## Delivery lifecycle

The supported consumer integrations create a delivery-scoped DI scope. When ITenantMessageContextInitializer is available, the consumer initializes tenant context before dispatch and disposes that context when processing exits, including failure or cancellation.

The initializer requires exactly one valid, non-empty tenant GUID header. The referenced tenant must exist and be active. If a domain header is present, it must identify a verified domain belonging to that tenant. The execution context is immutable and scoped to that delivery.

When RequireTenantScope is enabled, the Kafka and RabbitMQ hosted-consumer startup validation requires the tenant context integration and its authorization dependencies. Enable it for consumers that access tenant-owned data. It is disabled by default because the package also supports outbound metadata propagation.

## Metadata and accessors

- TenantContextAccessor.GetCurrentTenantId() and DomainContextAccessor.GetCurrentDomainId() read validated execution context. Their SetCurrentTenantId/SetCurrentDomainId methods write outbound message headers only; they do not mutate or establish the validated execution context.
- TenantMessageBuilder and DomainMessageBuilder add outbound metadata from the current validated execution context.
- HeaderTenantResolver and HeaderDomainResolver resolve header-based candidates.
- MessagePropertyTenantResolver and MessagePropertyDomainResolver resolve property-based candidates.

Use the explicit tenant and domain accessor methods. The Get methods read validated execution context; the Set methods only enrich the current outbound message. They cannot be used to establish tenant context. The 3.0.0 contract makes tenant and domain identity distinct at the API boundary.

## Security and operational requirements

- Do not trust message headers or properties as producer authorization.
- Do not reuse scoped handlers or tenant context across deliveries.
- Ensure the broker identity used by ITenantMessageAuthorizer is authenticated by the transport.
- Make message side effects idempotent; retries and redelivery can repeat delivery.
- Propagate cancellation into tenant-store lookups, validation, authorization, and handlers.

## Related packages

- Franz.Common.MultiTenancy — core tenant contracts and execution context.
- Franz.Common.Messaging — transport-neutral messaging contracts.
- Franz.Common.Messaging.Hosting.Kafka and Franz.Common.Messaging.Hosting.RabbitMQ — hosted consumer execution.
- Franz.Common.Http.MultiTenancy — HTTP tenant resolution and authorization.

## License

MIT. See the repository [LICENSE](../../LICENSE).
