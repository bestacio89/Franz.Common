# RabbitMQ operational contracts

Franz.Common keeps RabbitMQ transport mechanics in the RabbitMQ adapter and hosting packages. Applications supply message handlers and application-level idempotency; they do not reimplement connection management, delivery acknowledgement, or host lifecycle behavior.

## Lifecycle

- `RabbitMQHostedService` remains active for the lifetime of its consumer task.
- Listener startup completes only after RabbitMQ accepts the consumer registration.
- Startup and runtime failures propagate to the generic host rather than becoming unobserved background-task exceptions.
- Cancellation stops consumption. Deliveries not acknowledged before channel shutdown remain eligible for broker redelivery.
- A consumer handles deliveries sequentially per listener. Concurrency must be introduced explicitly, with channel ownership and acknowledgement ordering defined.

## Delivery and failure semantics

- A delivery is acknowledged only after the configured message callback completes successfully.
- A missing callback is a configuration error; the listener does not silently acknowledge and discard the delivery.
- A processing failure without a replay strategy is negatively acknowledged without requeue. A configured dead-letter exchange/queue determines whether the broker routes that message to a dead-letter destination.
- When a replay strategy completes successfully, the original delivery is acknowledged. If replay fails, the original delivery is negatively acknowledged without requeue and the failure is logged; a single replay failure must not silently terminate the consumer loop.
- These semantics are at-least-once oriented and can produce duplicate processing around failures. Application handlers must be idempotent where duplicate effects would be harmful. Franz.Common does not claim exactly-once business execution.

## Publishing and transactions

- The RabbitMQ transaction adapter must be attached to the channel leased for the current operation before a transaction begins.
- Publish/send cancellation tokens flow through message handling, publish operations, and transaction operations.
- If an operation fails and rollback also fails, both failures are preserved in the resulting exception.
- A successful application-level publish operation must not be interpreted as proof that a downstream consumer completed its business operation.
- RabbitMQ transactions and publisher confirms are distinct broker mechanisms and must not be treated as interchangeable. Any future publisher-confirm mode must explicitly define channel creation, timeout, unroutable-message, and transaction compatibility behavior.

## Security and configuration

- TLS certificate validation is strict by default. Missing certificates, hostname mismatches, and untrusted chains are not silently accepted.
- URI-based configuration must use an absolute `amqp://` or `amqps://` URI. If TLS is explicitly required, the URI must use `amqps://`.
- Connection recovery and heartbeat settings are sourced from the configured RabbitMQ options rather than silently replaced with hard-coded values.
- Heartbeat is a connection-level setting; it is not a queue argument.

## Topology and operations

- Queue and exchange names are derived from the application/event contracts unless explicitly configured.
- The default topology uses durable quorum queues and explicit dead-letter routing.
- Existing broker queues must be reviewed before changing their type or declaration arguments. RabbitMQ rejects incompatible redeclarations; topology changes may require an explicit migration rather than an in-place deployment.
- Integration tests should use a real RabbitMQ broker and assert observable outcomes: handler invocation, acknowledgement or redelivery, replay result, routing, shutdown, and failure propagation. Connectivity alone is not sufficient evidence of a correct messaging contract.
