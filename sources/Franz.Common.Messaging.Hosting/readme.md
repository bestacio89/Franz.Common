# Franz.Common.Messaging.Hosting

Generic hosted message execution and Outbox dispatch integration for Franz.Common.

## Install

```bash
dotnet add package Franz.Common.Messaging.Hosting --version 2.3.4
```

## Responsibilities

Provides hosted listener execution, message strategy selection and Outbox dispatch coordination. It owns the per-message dependency-injection scope and message-context lifetime for the generic hosted-service path.

When Franz.Common.Messaging.MultiTenancy is registered, the hosted handler initializes validated tenant execution before strategy selection and execution. Message context and tenant context are cleared/disposed after the full handler pipeline, including exceptions.

## Outbox lifecycle

OutboxMessageListener creates a fresh async DI scope for each stored message. It resolves message store, inbox store, dispatcher and serializer from that scope, initializes tenant context from the explicit message before callback/domain dispatch, then marks the message processed/sent after successful handling.

The listener clears ambient message context in a finally block. A nested hosted callback may clear its own ambient context; the Outbox path restores the current message context before subsequent domain dispatch.

## Constructor compatibility

The listener constructor requires IServiceScopeFactory and ILogger<OutboxMessageListener>. Applications that instantiate OutboxMessageListener manually must update construction to provide those services. Prefer dependency injection.

Framework release line: **2.3.4**; target framework: net10.0.

See [messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md).

MIT License.
