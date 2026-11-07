#nullable enable

namespace Franz.Common.Messaging.Sagas.Persistence;

/// <summary>
/// Persists saga state together with lifecycle metadata and an optimistic concurrency version.
/// Implementations must atomically compare the expected version and write the new record.
/// </summary>
public interface ISagaLifecycleRepository
{
  /// <summary>
  /// Loads a saga record or returns null when no record exists.
  /// </summary>
  Task<SagaPersistenceRecord?> LoadAsync(
      Guid sagaId,
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

  /// <summary>
  /// Deletes a record only when its current version matches the expected version.
  /// </summary>
  Task DeleteAsync(
      Guid sagaId,
      long expectedVersion,
      CancellationToken cancellationToken = default);
}
