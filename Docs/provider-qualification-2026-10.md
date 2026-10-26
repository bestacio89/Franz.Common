# EF Core MySQL provider qualification — October 2026

## Purpose

Qualify `Microting.EntityFrameworkCore.MySql` 10.0.13 against both MariaDB 11.4 and Oracle MySQL 8.4 before changing the production persistence integration. Passing one engine does not establish compatibility with the other.

## Real-engine suites

- `Franz.Common.EntityFramework.MariaDB.Tests`: MariaDB 11.4 in Testcontainers.
- `Franz.Common.EntityFramework.MySql.Tests`: MySQL 8.4 in Testcontainers.
- Both suites exercise the shared `DbContextBase` contract: create/update audit metadata, soft delete and query filters, tracked parent/child soft-delete behavior, relational query translation, relationship loading, and transaction commit/rollback.
- The MySQL suite additionally verifies server identity, schema creation, and generated SQL against the MySQL-specific server-version configuration.

## Regression gates

1. Restore with NuGet vulnerability auditing and pass the repository's vulnerability gate.
2. Build the complete `Franz.Common.slnx` in Release.
3. Run the complete solution test suite, including provider-neutral Entity Framework tests and the MariaDB/MySQL real-engine suites.
4. Review behavioral differences between MySQL 8.4 and MariaDB 11.4 instead of treating one engine's green result as proof for the other.
5. Before production migration, qualify migration generation/application and upgrade paths against a representative pre-existing schema. The current test models use `EnsureCreated`; they do not claim migration-upgrade coverage.
6. Only after those gates pass, assess the production provider registration and package changes in a focused follow-up.

## Known production-registration limitation

The current `AddMariaDatabase<TDbContext>` extension explicitly configures `ServerType.MariaDb` and a MariaDB 11.4 server version. The MySQL 8.4 qualification suite therefore configures `ServerType.MySql` directly; it does not claim that the existing registration extension supports Oracle MySQL. Decide whether the extension should become provider-neutral/configurable or remain MariaDB-specific as part of the production integration change, with an explicit regression test for the chosen contract.

## Security

Testcontainers-based suites pin `SSH.NET` 2026.0.0 explicitly to override the vulnerable transitive version previously reported by NuGet audit. Keep the repository-wide audit gate enabled.
