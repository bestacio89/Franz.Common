# Entity Framework repositories

## Set-based bulk updates

`IEntityFrameworkBulkUpdateRepository<TEntity>` exposes EF Core's provider-side `ExecuteUpdateAsync` capability through `EntityRepository`.

`ExecuteBulkUpdateAsync` composes the caller predicate over `DbContext.Set<TEntity>()`. EF Core global query filters therefore remain active, including Franz tenant and soft-delete filters. The operation returns the database-reported affected-row count.

Bulk updates execute directly in the database. They do not materialize entities and do not invoke `SaveChanges`, change-tracker auditing, SaveChanges interceptors, or domain-event dispatch. Callers must explicitly include any required modification/audit fields in the setter expression and must not assume tracked instances are refreshed.

For tenant-owned entity types, assignments to `TenantId` are rejected before execution. The current tenant query filter remains part of the generated update query. Do not use `IgnoreQueryFilters` or provider-specific raw SQL to bypass tenant isolation. EF Core's bulk API cannot make arbitrary raw SQL tenant-safe; raw SQL affecting tenant-owned data requires an independently enforced database policy (for example, database row-level security) or a separately reviewed tenant-scoped command.

This API is provider-capability dependent. Relational providers supporting EF Core `ExecuteUpdateAsync` can translate supported expressions. Non-relational providers, including Cosmos, may not support the operation and can throw the provider's standard unsupported-translation exception. It is intentionally not added to the provider-neutral business repository interfaces.
