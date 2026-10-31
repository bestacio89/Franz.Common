using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Franz.Common.EntityFramework.Repositories;

/// <summary>
/// Exposes provider-side set-based updates for relational EF Core repositories.
/// Bulk updates bypass change tracking, SaveChanges interceptors, auditing hooks and domain events.
/// Callers must set required audit values explicitly. Global query filters remain part of the query.
/// </summary>
public interface IEntityFrameworkBulkUpdateRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Executes a set-based update and returns the number of affected rows.
    /// Tenant-owned entities cannot have their TenantId changed through this operation.
    /// </summary>
    Task<int> ExecuteBulkUpdateAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
        CancellationToken cancellationToken = default);
}
