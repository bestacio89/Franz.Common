using System.Linq.Expressions;

namespace Franz.Common.EntityFramework.Repositories;

/// <summary>Exposes tenant-filtered set-based deletion for EF Core repositories.</summary>
public interface IEntityFrameworkBulkDeleteRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Executes an audited soft delete by default. Physical deletion is performed only
    /// when <paramref name="hardDelete"/> is explicitly set to <see langword="true"/>.
    /// </summary>
    Task<int> ExecuteBulkDeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool hardDelete = false,
        CancellationToken cancellationToken = default);
}
