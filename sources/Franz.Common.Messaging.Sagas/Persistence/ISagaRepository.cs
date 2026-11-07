#nullable enable

using Franz.Common.DependencyInjection;

namespace Franz.Common.Messaging.Sagas.Persistence;

/// <summary>
/// Persists saga state together with lifecycle metadata and an optimistic concurrency version.
/// Reads and deletes are scoped by tenant identity. Save operations must atomically compare
/// the expected version and write the new record.
/// </summary>
public interface ISagaRepository : IScopedDependency
{
  Task<SagaPersistenceRecord?> LoadAsync(
      Guid sagaId,
      string? tenantId,
      Type stateType,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Creates a record when <paramref name="expectedVersion"/> is null, or updates it only
  /// when the stored version equals the supplied expected version.
  /// The returned record contains the incremented version.
  /// </summary>
  Task<SagaPersistenceRecord> SaveAsync(
      SagaPersistenceRecord record,
      long? expectedVersion,
      CancellationToken cancellationToken = default);

  Task DeleteAsync(
      Guid sagaId,
      string? tenantId,
      long expectedVersion,
      CancellationToken cancellationToken = default);
}
