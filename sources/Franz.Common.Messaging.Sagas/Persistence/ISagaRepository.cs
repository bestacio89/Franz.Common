#nullable enable

using Franz.Common.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Persistence;

/// <summary>
/// Provides persistence for saga state. 
/// Implementations can use EF, Redis, Kafka compacted topics, or memory.
/// </summary>
public interface ISagaRepository : IScopedDependency
{
  /// <summary>
  /// Loads a saga state for the given ID and type, or returns null if not found.
  /// </summary>
  Task<object?> LoadStateAsync(string sagaId, Type stateType, CancellationToken cancellationToken);

  /// <summary>
  /// Saves the saga state. Creates or updates the record.
  /// </summary>
  Task SaveStateAsync(string sagaId, object state, CancellationToken cancellationToken);

  /// <summary>
  /// Deletes a saga state when the saga completes.
  /// Not all implementations must support delete.
  /// </summary>
  Task DeleteStateAsync(string sagaId, CancellationToken cancellationToken);
}
