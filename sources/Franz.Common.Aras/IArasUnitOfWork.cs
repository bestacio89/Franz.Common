using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Business;

namespace Franz.Common.Aras
{
  public interface IArasUnitOfWork : IDisposable
  {
    IArasEntityContext Entities { get; }
    IArasAggregateContext Aggregates { get; }

    /// <summary>
    /// Commits all tracked entity and aggregate changes as one atomic operation.
    /// </summary>
    Task<int> CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Rolls back tracked changes. No operations are persisted.
    /// </summary>
    Task RollbackAsync(CancellationToken ct = default);
  }
}
