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
  public Task<object?> LoadStateAsync(Guid sagaId, Type stateType, CancellationToken cancellationToken)
      => throw new NotImplementedException("Redis saga persistence is not implemented yet.");

  public Task SaveStateAsync(Guid sagaId, object state, CancellationToken cancellationToken)
      => throw new NotImplementedException("Redis saga persistence is not implemented yet.");

  public Task DeleteStateAsync(Guid sagaId, CancellationToken cancellationToken)
      => Task.CompletedTask;
}
