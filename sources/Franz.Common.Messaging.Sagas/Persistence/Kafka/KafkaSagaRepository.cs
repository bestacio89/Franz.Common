#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Persistence.Kafka;

/// <summary>
/// Kafka persistence using a compacted topic (to implement later).
/// </summary>
public sealed class KafkaSagaRepository : ISagaRepository
{
  public Task<object?> LoadStateAsync(Guid sagaId, Type stateType, CancellationToken cancellationToken)
      => throw new NotImplementedException("Kafka saga persistence is not implemented yet.");

  public Task SaveStateAsync(Guid sagaId, object state, CancellationToken cancellationToken)
      => throw new NotImplementedException("Kafka saga persistence is not implemented yet.");

  public Task DeleteStateAsync(Guid sagaId, CancellationToken cancellationToken)
      => Task.CompletedTask; // compacted topics usually ignore deletes
}
