# EF Core MySQL provider qualification — October 2026

## Purpose

Qualify `Microting.EntityFrameworkCore.MySql` 10.0.13 against MariaDB 11.4 and Oracle MySQL 8.4 before relying on it as Franz.Common's EF Core provider. Passing one engine does not establish compatibility with the other. The qualification is evidence for the provider substitution, not a claim that every application schema or migration has been exercised.

## Real-engine suites

- `Franz.Common.EntityFramework.MariaDB.Tests`: MariaDB 11.4 in Testcontainers.
- `Franz.Common.EntityFramework.MySql.Tests`: Oracle MySQL 8.4 in Testcontainers.
- Both suites exercise the shared `DbContextBase` contract: create/update audit metadata, soft delete and query filters, tracked parent/child soft-delete behavior, relational query translation, relationship loading, and transaction commit/rollback.
- The MySQL suite additionally verifies server identity, schema creation, and generated SQL against the MySQL-specific server-version configuration.
- Both provider suites include an incremental migration test: generate a two-step migration script, apply the initial schema, insert a pre-existing row, apply a schema-expansion migration, and assert that the row survives, the new column exists, and both migrations are recorded in the provider's migration history table.

## Regression and release gates

1. Restore with NuGet vulnerability auditing and pass the repository's vulnerability gate; verify the audit step actually ran rather than relying on a skipped step.
2. Build the complete `Franz.Common.slnx` in Release.
3. Run the complete solution test suite, including provider-neutral Entity Framework tests and both real-engine suites.
4. Review behavioral differences between MySQL 8.4 and MariaDB 11.4 instead of treating one engine's green result as proof for the other.
5. Treat the incremental migration test as provider-level evidence only. Before claiming application-specific migration compatibility, run the application's actual migration chain against a representative pre-change database/schema and verify schema plus data preservation. The current Franz.Common provider tests do not substitute for an application-owned migration history or snapshot.
6. Validate package metadata/versioning and produce the release artifacts for Franz.Common 2.3.1 only after all gates pass.

## Known production-registration limitation

The current `AddMariaDatabase<TDbContext>` extension explicitly configures `ServerType.MariaDb` and MariaDB 11.4. The MySQL 8.4 qualification suite configures `ServerType.MySql` directly; it does not claim that the existing registration extension supports Oracle MySQL. Keep the extension MariaDB-specific unless there is a concrete requirement to support Oracle MySQL through the same public registration API. Any API change should be tested as a separate contract.

## Security

Testcontainers-based suites pin `SSH.NET` 2026.0.0 explicitly to override the vulnerable transitive version previously reported by NuGet audit. Keep the repository-wide audit gate enabled.
