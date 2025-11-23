# Messaging Flow – Franz.Common

This document details the end-to-end lifecycle of messages in Franz.Common services. It applies to Kafka and RabbitMQ providers.

---

## 1. Goals

Franz.Messaging enforces:

- Reliability (retries, DLQs)
- Traceability (correlation, causation)
- Idempotency (optional inbox/outbox)
- Isolation (no business logic in transport)
- Extensibility (custom middlewares, interceptors)

---

## 2. Message Envelope

All messages are wrapped in a standardized envelope:

- MessageId
- CorrelationId
- CausationId
- Timestamp
- Payload
- Headers

This enables monitoring, tracing, error reporting, and cross-service debugging.

---

## 3. Producer Workflow

1. Application handler calls `IFranzMessageBus.PublishAsync(message)`
2. Franz enriches message with metadata (correlation ID, timestamps)
3. Message is serialized using the configured serializer
4. Message is either:
   - sent directly to the broker  
   - *or* stored in the **Outbox** and sent asynchronously
5. Success metrics & logs are emitted

---

## 4. Consumer Workflow

1. Kafka/RabbitMQ consumer receives a raw message  
2. Franz deserializes envelope + payload  
3. Metadata is restored into a `MessageContext`  
4. Handler is executed inside:
   - try/catch wrapper  
   - retry logic  
   - cancellation token management  
5. Result is acknowledged or rejected  
6. On permanent failure → DLQ routing  
7. Metrics & logs are updated

---

## 5. Retry Policy

Recommended defaults:

- 3–5 attempts  
- exponential backoff (200ms → 2s → 10s)  
- final routing to DLQ  

Optionally override with policies per message type.

---

## 6. DLQ Behavior

DLQ messages include:

- Original payload  
- Error message  
- Exception stack (optional)  
- Retry count  
- Timestamps  
- CorrelationId  

DLQ events may be monitored by:

- Grafana dashboards  
- ELK queries  
- Dedicated DLQ processors  

---

## 7. Idempotency

Franz supports:

- Inbox table (hash-based deduplication)
- Outbox table (atomic write + async dispatch)

This is essential for financial/government workflows.

---

## 8. Observability

Every message produces:

- `MessageReceived`
- `MessageProcessed`
- `MessageFailed`
- `MessageDelayed`
- `MessageDeadLettered`

with correlation IDs attached.

---

## 9. Summary

Franz’s messaging layer is designed for high-reliability distributed systems with auditability, correctness, and resilience as first-class citizens.
