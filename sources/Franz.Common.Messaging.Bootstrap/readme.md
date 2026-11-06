# Franz.Common.Messaging.Bootstrap

> Composition helper for common Franz architecture plus messaging identity, tenant, and header contexts.

## Overview

Composition helper for common Franz architecture plus messaging identity, tenant, and header contexts.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Bootstrap
```

## Registration

The current extension is `AddMessagingArchitecture` in the namespace `Franz.Common.Messaging.Bootstrap.Extenstions` (the namespace spelling is present in the source).

```csharp
using Franz.Common.Messaging.Bootstrap.Extenstions;

builder.Services.AddMessagingArchitecture(
    builder.Environment,
    builder.Configuration,
    typeof(Program).Assembly);
```

The assembly parameter is optional; when omitted, the method uses its calling assembly. Pass the application assembly explicitly to make scanning deterministic.

## Composition performed

The method delegates to `AddCommonArchitecture(configuration, assembly)`, then registers:

- `AddMessagingIdentityContext()`
- `AddMessagingMultitenancyContext()`
- `AddMessagingHeaderContext()`

The common architecture helper performs the reflection, mediator, and marker-based dependency registrations documented by `Franz.Common.Bootstrap`. The three messaging context registrations add the relevant message-scoped context accessors.

## Important boundaries

This method does not register Kafka, RabbitMQ, Azure, or another broker transport. It also does not select a messaging provider from configuration. Add the broker-specific registration from the corresponding provider package.

The `IHostEnvironment` parameter is present in the current signature but is not read by the implementation. Do not expect environment-dependent registration behavior from it.

## Composition guidance

Use this package to compose the common messaging context layer, not as a substitute for provider setup, host worker registration, message-store configuration, or transport provisioning. Keep transport-specific decisions in the application composition root.

## License

MIT. See [LICENSE](../../LICENSE).
