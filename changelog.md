# Franz Framework — Full Changelog

## v2.3.4 — Multi-tenancy execution and relational qualification

### Tenant isolation and relational persistence
- Added a shared real-engine tenant-isolation contract for PostgreSQL 16, MariaDB 11.4, MySQL 8.4, SQL Server 2022 and Oracle Free 23.
- Qualified tenant-scoped reads/writes, missing-context fail-closed behaviour, cross-tenant insert/update rejection and tenant ownership immutability.
- Verified EF Core set-based update/delete query-filter behaviour and framework repository bulk-update tenant-filter/ownership protections.
- Expanded documentation for raw SQL, stored procedures, query-filter bypasses, provider-native bulk operations and database-level enforcement responsibilities.

### Messaging execution boundary
- Added explicit-message tenant context initialization with a fail-closed default interface implementation for custom initializers that have not implemented the new overload.
- Registered tenant/domain message builders through AddMessagingMultitenancyContext; outbound metadata is derived from validated execution context and stale domain metadata is removed.
- Integrated tenant initialization into generic messaging, Kafka, RabbitMQ, Azure Service Bus, Azure Event Hubs, Outbox and MassTransit Kafka consumer paths.
- Established per-delivery dependency-injection scopes and tenant execution lifetimes, including cleanup on success, exception and cancellation.
- Refactored Outbox processing to resolve persistence and dispatch services from a fresh scope per stored message.
- Added a scoped-handler MassTransit Kafka overload and fail-closed behaviour for the legacy captured-handler overload when tenant-aware processing is enabled.
- Added lifecycle, failure, concurrency, context cleanup and metadata propagation tests.

### Package and release baseline
- Updated shared package version to 2.3.4 and file version to 2.3.4.0; assembly version remains 2.3.0.0.
- Updated main and affected package READMEs and central architecture/security documentation.
- NuGet publication remains a separate release operation.

## v2.3.3 — RabbitMQ Operational Hardening
- Supervised consumer startup and runtime failure through the generic host.
- Defined single-owner acknowledgement and replay behaviour, bounded delivery buffering, and safer channel/connection lifecycle management.
- Enforced strict TLS validation by default and clarified transaction-channel ownership.
- Documented operational contracts and added real-broker dead-letter routing coverage.

## v2.3.2 — Release Baseline
- Coordinated package baseline following .NET 10 dependency maintenance and provider qualification.

## v2.3.0 — Source Generated Runtime and Execution Model Modernization
- Added compile-time handler registration through Roslyn source generators.
- Retained compatibility registration for migration scenarios and recommended generated registration for modern applications.
- Improved deterministic dependency registration, startup behaviour and Native AOT readiness.

Older release history is preserved in repository tags.
