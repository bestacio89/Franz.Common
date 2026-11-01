# Tenant isolation boundary audit

## Guarantees exercised by automated tests

The PostgreSQL integration suite exercises the framework-managed EF Core boundary:

- Tenant-owned queries are filtered to the current execution tenant.
- Queries without a tenant execution context fail closed and return no tenant-owned rows.
- Writes to tenant-owned entities require a tenant execution context.
- Inserts cannot claim a tenant other than the current execution tenant.
- Tracked and detached writes are checked against the current tenant.
- Bulk updates retain EF Core global query filters and cannot assign a new `TenantId`.
- HTTP and messaging execution accessors reject accidental context replacement and support explicit cleanup.
- Message initialization rejects unauthorized producers and invalid or foreign domain metadata.

These tests establish behavior for the tested code paths and providers; they are not a claim that every external database access path is tenant-safe.

## Explicit trust boundaries

### Raw SQL and direct database access

EF Core global query filters do not rewrite raw SQL, stored procedures, direct ADO.NET commands, provider-native bulk APIs, or connections opened outside the framework's repository/context path. A tenant ID included in an HTTP header, message header, or arbitrary job payload is untrusted until resolved and authorized by the relevant adapter.

Do not use raw SQL to access tenant-owned tables unless the command applies a trusted tenant predicate or the database independently enforces row isolation. Prefer database row-level security where supported. If tenant identity is applied through connection/session state, initialize it from the validated execution context and reset it reliably before a pooled connection is returned. Verify pooled-connection reuse and failure paths. Always parameterize values; never interpolate tenant-controlled values into SQL.

The framework does not provide a universal raw-SQL tenant-isolation guarantee. Database credentials and deployment controls must prevent application paths from silently bypassing the chosen boundary.

### Background work and message consumers

Create a fresh dependency-injection scope for each message/job. Re-resolve and authorize tenant identity from trusted metadata, initialize the tenant execution context for the duration of the operation, and dispose/clear that context on success, exception, and cancellation. Never capture an HTTP-scoped accessor or reuse a mutable execution accessor between jobs.

The library's context lifecycle tests do not prove that every third-party consumer or job scheduler in an application follows this contract; each adapter integration must be covered by its own tests.

## Provider qualification

| Capability | PostgreSQL | MariaDB | MySQL | SQL Server / Oracle | Cosmos |
|---|---|---|---|---|---|
| Tenant-filtered EF reads and writes | Integration tests required and present in PostgreSQL suite | Must be qualified against real provider | Must be qualified against real provider | Must be qualified against real provider | Separate partition/SDK semantics |
| EF Core set-based bulk update | Integration tests required and present in PostgreSQL suite | Verify provider/version and translated expressions | Verify provider/version and translated expressions | Verify provider/version and translated expressions | Do not assume `ExecuteUpdateAsync` support |
| Raw SQL isolation | Database policy or trusted tenant predicate required | Database policy or trusted tenant predicate required | Database policy or trusted tenant predicate required | Database policy or trusted tenant predicate required | Not equivalent to relational raw SQL |

Provider routing/configuration tests are not substitutes for real-engine tenant-isolation tests. Do not mark a provider qualified until tests prove cross-tenant read/write isolation, missing-context fail-closed behavior, ownership immutability, bulk-operation behavior where supported, and connection/session reset behavior where applicable.

## Release gate

- [ ] PostgreSQL tenant isolation integration suite passes in CI.
- [ ] Existing HTTP and messaging lifecycle tests pass.
- [ ] Bulk-update tenant-filter and ownership-mutation tests pass.
- [ ] Every supported relational provider has a named real-engine qualification result, or is explicitly documented as unqualified.
- [ ] Raw SQL, stored procedure, and direct-connection paths are inventoried by each consuming application and either prohibited for tenant-owned data or independently protected and tested.
- [ ] Consumer/job adapters prove tenant context cleanup after success, exception, and cancellation.
- [ ] Documentation does not imply global query filters protect paths that bypass EF Core.

A checked release gate is evidence only for the exact commit, provider versions, and execution paths that were tested.


## Release 2.3.4 evidence boundary

The 2.3.4 relational qualification names PostgreSQL 16, MariaDB 11.4, MySQL 8.4, SQL Server 2022 and Oracle Free 23. Qualification is limited to the framework-managed EF Core and repository paths covered by the shared contract suite. It must not be read as certification of arbitrary SQL, stored procedures, provider-native bulk APIs, direct database clients, or non-relational providers. Each consuming application remains responsible for inventorying and independently securing those escape hatches.
