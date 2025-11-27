#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Persistence.Redis;

/// <summary>
/// Redis-based saga persistence (to implement later).
/// </summary>
public sealed class RedisSagaRepository : ISagaRepository
{
  public Task<object?> LoadStateAsync(string sagaId, Type stateType, CancellationToken cancellationToken)
      => throw new NotImplementedException("Redis saga persistence is not implemented yet.");

  public Task SaveStateAsync(string sagaId, object state, CancellationToken cancellationToken)
      => throw new NotImplementedException("Redis saga persistence is not implemented yet.");

  public Task DeleteStateAsync(string sagaId, CancellationToken cancellationToken)
      => Task.CompletedTask;
}
