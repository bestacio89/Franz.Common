# Franz.Common.Messaging.Identity

> Message-context identity access and authorization-header enrichment for Franz messaging envelopes.

## Overview

Message-context identity access and authorization-header enrichment for Franz messaging envelopes.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Identity
```

## Register the identity accessor

```csharp
using Franz.Common.Messaging.Identity.Extensions;

services.AddMessagingIdentityContext();
```

This registers `IIdentityContextAccessor` as a scoped `IdentityContextAccessor` using the no-duplicate scoped registration helper.

## Read identity from the current message

`IdentityContextAccessor` reads identity metadata from `IMessageContextAccessor.Current.Message.Headers`. It exposes:

- `GetCurrentEmail()`
- `GetCurrentId()`
- `GetCurrentFullName()`
- `GetCurrentTenantId()`
- `GetCurrentDomainId()`
- `GetCurrentRoles()`
- `GetCurrentIdentity()`

User, tenant, and domain identifiers are parsed as GUIDs. Roles are returned as the string array stored in the message headers. Missing message context or missing headers produce null values or an empty role array. `GetCurrentIdentity()` returns null when there is no current message context; otherwise it constructs a `FranzIdentityContext` from the available fields.

## Authorization header enrichment

`AuthorizationMessageBuilder` implements `IMessageBuilder`. It accepts an optional `IHeaderContextAccessor`. `CanBuild` returns true when that accessor is available. `BuildAsync` copies non-empty values from the current Authorization header into the outgoing message's headers.

The builder performs a memory copy and currently returns a completed task. It does not validate, refresh, or exchange the credential.

## Security model

Authorization propagation is credential delegation. Only enable it when the downstream consumer is intended to act with the originating caller's authority. Define token audience, expiry, and trust boundaries explicitly; do not forward credentials to arbitrary brokers or downstream services. Avoid logging the Authorization header or persisting it in message payloads.

## Package boundaries

This package adapts identity and header context to messaging. It does not authenticate the original HTTP request, authorize message handlers, or validate tenant ownership. Those responsibilities remain in the identity provider and consuming application.

## License

MIT. See [LICENSE](../../LICENSE).
