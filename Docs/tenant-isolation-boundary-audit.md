# Tenant isolation boundary audit

## Scope and evidence

This document describes the framework-managed EF Core tenant boundary and the application-controlled paths around it. Automated relational contract tests exercise PostgreSQL 16, MariaDB 11.4, MySQL 8.4, SQL Server 2022 and Oracle Free 23 using real database engines.

The contract is evidence for the tested code paths and exact provider versions. It is not a claim that every database access path, application or deployment is tenant-safe.

## Framework-managed EF Core guarantees

For entities implementing ITenantEntity and exposing a mapped Guid TenantId, the configured DbContextBase tenant accessor enables:

- Tenant-owned reads filtered to the validated execution tenant.
- Fail-closed reads when no tenant execution context is available.
- Rejection of tenant-owned writes without an execution tenant.
- Automatic stamping of new tenant-owned entities when TenantId is empty.
- Rejection of inserts and tracked/detached writes that claim another tenant.
- Protection against changing tenant ownership after insertion.
- Tenant filters retained by EF Core ExecuteUpdateAsync and ExecuteDeleteAsync.
- Tenant-filter preservation and TenantId assignment rejection in the framework repository ExecuteBulkUpdateAsync implementation.

The shared provider contract verifies these behaviours on each qualified relational engine.

## HTTP and message execution boundaries

HTTP tenant context is established only after resolution, lifecycle validation and authenticated-principal membership authorization. Request headers and claims are resolution inputs, not standalone proof of membership.

Messaging tenant metadata is untrusted transport input. The consumer resolves the tenant from the tenant store, requires active status, applies configured validation, authorizes the producer, validates domain ownership/verification and only then initializes execution context. Hosted consumer and Outbox paths use per-delivery scopes and clear context after processing, including failure and cancellation paths.

See [messaging execution lifecycle](../docs/multitenancy-execution-lifecycle.md).

## Explicit bypasses and application responsibilities

EF Core global query filters do not rewrite or secure raw SQL, stored procedures, direct database connections, external database clients, provider-native bulk APIs or queries that deliberately call IgnoreQueryFilters.

Such paths require a trusted tenant predicate or independent database enforcement such as row-level security. If tenant identity is applied through connection/session state, initialize it from validated execution context and reset it before a pooled connection is returned. Parameterize all values; never interpolate tenant-controlled values into SQL.

Tenant-scoped unique constraints, composite foreign keys and database permissions remain application/schema responsibilities. The framework does not rewrite business uniqueness semantics.

## Provider qualification

| Provider | Test engine | Shared tenant contract |
|---|---|---|
| PostgreSQL | PostgreSQL 16 | Passed in release qualification |
| MariaDB | MariaDB 11.4 | Passed in release qualification |
| MySQL | MySQL 8.4 | Passed in release qualification |
| SQL Server | SQL Server 2022 | Passed in release qualification |
| Oracle | Oracle Free 23 | Passed in release qualification |
| Cosmos DB | Separate SDK/provider model | Not covered by this relational contract |
| MongoDB | Separate document/provider model | Not covered by this relational contract |

The shared contract is necessary, not sufficient, for provider interchangeability. Each provider must also pass its migration, CRUD, transaction, concurrency, configuration, value-conversion and SQL-translation suites.

## Release and deployment checklist

- [ ] Run the shared tenant contract against the exact database image/version used by the release.
- [ ] Run provider-specific migration, transaction, CRUD and concurrency suites.
- [ ] Verify derived DbContexts forward ITenantExecutionContextAccessor to DbContextBase.
- [ ] Register application-specific HTTP membership and message producer authorizers.
- [ ] Inventory raw SQL, stored procedures, query-filter bypasses and provider-native bulk paths.
- [ ] Protect bypasses with trusted tenant predicates or database enforcement and dedicated tests.
- [ ] Verify tenant context cleanup on success, exception and cancellation.
- [ ] Test pooled connection/session state reset if database session tenancy is used.
- [ ] Keep tenant provisioning, suspension, deactivation, backup/recovery and database permissions in the operational runbook.

A checked list is evidence only for the exact commit, provider versions, configuration and paths tested.
