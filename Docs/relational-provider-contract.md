# Relational EF Core provider contract

## Purpose

Relational EF Core adapters must preserve framework-level persistence and tenant-isolation behaviour when an application changes its configured provider. Provider-specific SQL, migrations and server capabilities remain implementation details only where the contract proves equivalent behaviour.

The shared contract is linked into each provider's real-engine test project. It is not an EF InMemory or SQLite substitute.

## Qualified providers

| Provider | Test engine | Shared tenant contract |
|---|---|---|
| PostgreSQL | PostgreSQL 16 Testcontainers | Required |
| MariaDB | MariaDB 11.4 Testcontainers | Required |
| MySQL | MySQL 8.4 Testcontainers | Required |
| SQL Server | SQL Server 2022 Testcontainers | Required |
| Oracle | Oracle Free 23 Testcontainers | Required |

Each qualification run must report the actual server image/version. A pass on one provider does not certify another provider or a different provider version.

## Shared behavioural contract

Every provider suite executes the same tenant-owned persistence scenarios against its real database engine:

- Tenant-owned reads return only rows belonging to the validated execution tenant.
- Queries without an execution tenant fail closed.
- Tenant-owned writes without an execution tenant are rejected.
- Inserts cannot claim another tenant's identity.
- Tracked and detached updates cannot cross tenant ownership boundaries.
- Tenant identity cannot be reassigned after insertion.
- EF Core ExecuteUpdateAsync and ExecuteDeleteAsync retain configured global tenant query filters.
- The framework repository ExecuteBulkUpdateAsync retains tenant filtering and rejects assignments to TenantId.

The shared suite uses a dedicated tenant-owned contract table to avoid collisions with provider-specific fixtures.

## Additional provider qualification

The shared tenant contract is necessary, not sufficient, for provider interchangeability. Each provider CI qualification must also retain and pass provider-specific suites for:

1. Schema creation and migrations from an empty database, plus upgrade from the previous supported schema.
2. CRUD, key generation, auditing, soft-delete filters and relationship/cascade behaviour.
3. Transaction commit, rollback, cancellation and failure recovery.
4. Repository range operations and unit-of-work semantics.
5. Set-based update/delete translation, affected-row counts and tenant-filter preservation.
6. Optimistic concurrency and provider-specific exception translation.
7. Connection-string configuration, isolation-mode routing, disposal and pooling.
8. Supported value conversions, precision, nullability, indexes and generated SQL.

Unsupported capabilities must be explicitly documented. They must not be silently skipped or represented as provider interchangeability.

## Security boundary

EF Core global query filters constrain EF queries and supported set-based operations that retain those filters. They do not secure arbitrary SQL, stored procedures, direct ADO.NET commands, provider-native bulk APIs or connections that bypass the configured DbContext.

SaveChanges ownership checks do not run for direct EF Core ExecuteUpdateAsync calls because those operations bypass the change tracker and SaveChanges pipeline. The framework EntityRepository.ExecuteBulkUpdateAsync uses the bulk-update builder to reject direct TenantId assignments and is covered by the shared contract. Direct low-level set-based calls must not be used to mutate tenant ownership. Add database enforcement when the application must defend against code paths that bypass the framework repository.

## Non-relational providers

Cosmos DB and MongoDB are not part of this relational contract. Their partition-key, document ownership, transaction and bulk-operation semantics require separate provider-specific contracts. Passing this suite does not certify those providers.
