using System.Linq.Expressions;

namespace Franz.Common.EntityFramework.Repositories;

/// <summary>Exposes provider-side set-based updates for EF Core repositories.</summary>
public interface IEntityFrameworkBulkUpdateRepository<TEntity> where TEntity : class
{
    /// <summary>Executes a set-based update and returns the affected-row count.</summary>
    Task<int> ExecuteBulkUpdateAsync(
        Expression<Func<TEntity, bool>> predicate,
        Action<BulkUpdateBuilder<TEntity>> configure,
        CancellationToken cancellationToken = default);
}
