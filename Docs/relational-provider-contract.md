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
- The framework repository's `ExecuteBulkUpdateAsync` respects tenant filters, rejects assignments to `TenantId` and `ConcurrencyVersion`, and advances opt-in concurrency tokens.
- `ConcurrencyTrackedEntity<TKey>` uses provider-translated optimistic concurrency; stale writes fail with `DbUpdateConcurrencyException`.
- Repository bulk delete defaults to audited soft delete, advances concurrency tokens, and remains tenant-scoped; physical deletion requires an explicit `hardDelete: true`.

The contract uses a dedicated table (`provider_contract_tenant_owned_entities`) to avoid collisions with provider-specific persistence fixtures.

## Additional qualification required before claiming interchangeability

The shared contract is necessary, not sufficient. Each provider's CI qualification must also retain and pass its provider-specific suites for:

1. Schema creation and migration application from an empty database, followed by an upgrade from the previous supported schema.
2. CRUD, key generation, auditing, soft-delete filters, and relationship/cascade behavior.
3. Transaction commit, rollback, cancellation, and failure recovery.
4. Repository AddRange, UpdateRange, DeleteRange, and SoftDeleteRange semantics.
5. Set-based update/delete translation, affected-row counts, tenant-filter preservation, audit metadata, and concurrency-token advancement.
6. Optimistic concurrency behavior through `ConcurrencyTrackedEntity<TKey>` and provider-specific exception translation.
7. Connection-string configuration, isolation-mode routing, and safe connection disposal/pooling.
8. Supported value conversions, precision, nullability, indexes, and generated SQL for framework-supported model features.

A capability that is unsupported by a provider must be explicitly documented and represented as a capability difference. It must not be silently skipped or reported as provider interchangeability.

## Security boundary

EF Core global query filters constrain EF queries, including supported set-based operations that retain those filters. They do not secure arbitrary SQL, stored procedures, direct ADO.NET commands, provider-native bulk APIs, or connections that bypass the configured DbContext. Those paths require trusted tenant predicates or independent database enforcement and dedicated tests.

The SaveChanges tenant ownership checks do not automatically run for direct EF Core `ExecuteUpdateAsync` calls because those operations bypass the change tracker and SaveChanges pipeline. The framework's `EntityRepository.ExecuteBulkUpdateAsync` uses `BulkUpdateBuilder<TEntity>` to reject direct assignments to `TenantId`; the shared contract verifies that protection and tenant-filtered affected-row behavior. Direct EF Core set-based calls remain a lower-level escape hatch and must not be used to mutate tenant ownership. They also bypass SaveChanges auditing and framework-managed concurrency-version advancement; use the framework repository operations when those guarantees are required. Database enforcement is required if applications must defend against code paths that bypass the framework repository.

## Non-relational providers

Cosmos DB and MongoDB are not part of this relational contract. Their partition-key, document ownership, transaction, and bulk-operation semantics require separate provider-specific contracts. Passing this suite must not be interpreted as certifying those providers.


## 2.3.4 qualification scope and limits

The release qualification covers PostgreSQL 16, MariaDB 11.4, MySQL 8.4, SQL Server 2022 and Oracle Free 23 for the shared relational EF Core contract. The tested behavior includes tenant-scoped reads and writes, missing-context fail-closed behavior, tenant ownership immutability, cross-tenant mutation rejection, supported EF Core set-based query-filter behavior, and repository bulk-update filtering/ownership validation.

This is not a blanket certification of every database feature. Provider-specific raw SQL, stored procedures, direct connections, native bulk libraries, and code that disables or bypasses EF Core query filters sit outside the framework-managed guarantee unless separately guarded and tested. Applications with a high-assurance isolation requirement should apply database permissions and database-native enforcement in addition to application-layer checks. Cosmos DB and MongoDB remain outside this relational contract.
