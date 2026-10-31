# Relational EF Core provider contract

## Purpose

The relational EF Core providers must preserve the same framework-level persistence and tenant-isolation behavior when an application changes its configured provider. Provider-specific SQL, migrations, and server capabilities remain implementation details unless explicitly documented as capability differences.

The shared contract is linked into each real-engine test project. It is not an InMemory or SQLite substitute.

## Providers under qualification

| Provider | Test engine | Shared tenant contract | Provider-specific suites |
|---|---|---:|---|
| PostgreSQL | PostgreSQL 16 Testcontainers | Required | Persistence, migrations, provider registration |
| MariaDB | MariaDB 11.4 Testcontainers | Required | Persistence lifecycle, migrations, transactions, registration |
| MySQL | MySQL 8.4 Testcontainers | Required | Persistence lifecycle, migrations, transactions, SQL compatibility |
| SQL Server | SQL Server 2022 Testcontainers | Required | Persistence, soft delete, database-isolation routing |
| Oracle | Oracle Free 23 Testcontainers | Required | Connectivity, tenant/domain database routing |

Each test run must report the actual server image/version. A passing contract on one provider does not certify another provider.

## Shared behavioral contract

Every provider suite executes the same tests against its real database engine:

- Tenant-owned reads return only rows belonging to the validated execution tenant.
- Queries without an execution tenant fail closed.
- Tenant-owned writes without an execution tenant throw `TenantIsolationException`.
- Inserts cannot claim another tenant's identity.
- EF Core `ExecuteUpdateAsync` respects the global tenant query filter.
- EF Core `ExecuteDeleteAsync` respects the global tenant query filter.

The contract uses a dedicated table (`provider_contract_tenant_owned_entities`) to avoid collisions with provider-specific persistence fixtures.

## Additional qualification required before claiming interchangeability

The shared contract is necessary, not sufficient. Each provider's CI qualification must also retain and pass its provider-specific suites for:

1. Schema creation and migration application from an empty database, followed by an upgrade from the previous supported schema.
2. CRUD, key generation, auditing, soft-delete filters, and relationship/cascade behavior.
3. Transaction commit, rollback, cancellation, and failure recovery.
4. Repository AddRange, UpdateRange, DeleteRange, and SoftDeleteRange semantics.
5. Set-based update/delete translation, affected-row counts, and tenant-filter preservation.
6. Optimistic concurrency behavior and provider-specific exception translation.
7. Connection-string configuration, isolation-mode routing, and safe connection disposal/pooling.
8. Supported value conversions, precision, nullability, indexes, and generated SQL for framework-supported model features.

A capability that is unsupported by a provider must be explicitly documented and represented as a capability difference. It must not be silently skipped or reported as provider interchangeability.

## Security boundary

EF Core global query filters constrain EF queries, including supported set-based operations that retain those filters. They do not secure arbitrary SQL, stored procedures, direct ADO.NET commands, provider-native bulk APIs, or connections that bypass the configured DbContext. Those paths require trusted tenant predicates or independent database enforcement and dedicated tests.

The SaveChanges tenant ownership checks do not automatically run for EF Core set-based operations because those operations bypass the change tracker and SaveChanges pipeline. The shared contract therefore verifies tenant row filtering for set-based update/delete; it does not claim that arbitrary set-based updates cannot mutate the TenantId column. Do not expose tenant-ownership mutation through bulk APIs unless that API independently rejects it or the database enforces the invariant.

## Non-relational providers

Cosmos DB and MongoDB are not part of this relational contract. Their partition-key, document ownership, transaction, and bulk-operation semantics require separate provider-specific contracts. Passing this suite must not be interpreted as certifying those providers.
