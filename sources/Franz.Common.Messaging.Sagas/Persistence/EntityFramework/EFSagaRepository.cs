#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

/// <summary>
/// Entity Framework-based saga repository.
/// </summary>
public sealed class EFSagaRepository : ISagaRepository
{
  private readonly SagaDbContext _db;
  private readonly ISagaStateSerializer _serializer;

  public EFSagaRepository(SagaDbContext db, ISagaStateSerializer serializer)
  {
    _db = db;
    _serializer = serializer;
  }

  public async Task<object?> LoadStateAsync(
      string sagaId,
      Type stateType,
      CancellationToken cancellationToken)
  {
    var entity = await _db.SagaStates
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.SagaId == sagaId, cancellationToken);

    if (entity == null)
      return null;

    return _serializer.Deserialize(entity.SerializedState, stateType);
  }

  public async Task SaveStateAsync(
      string sagaId,
      object state,
      CancellationToken cancellationToken)
  {
    var serialized = _serializer.Serialize(state);

    var entity = await _db.SagaStates
        .FirstOrDefaultAsync(x => x.SagaId == sagaId, cancellationToken);

    if (entity == null)
    {
      entity = new SagaStateEntity
      {
        SagaId = sagaId,
        SagaType = state.GetType().FullName ?? state.GetType().Name,
        SerializedState = serialized
      };

      _db.SagaStates.Add(entity);
    }
    else
    {
      entity.SerializedState = serialized;
    }

    await _db.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteStateAsync(
      string sagaId,
      CancellationToken cancellationToken)
  {
    var entity = await _db.SagaStates
        .FirstOrDefaultAsync(x => x.SagaId == sagaId, cancellationToken);

    if (entity != null)
    {
      _db.SagaStates.Remove(entity);
      await _db.SaveChangesAsync(cancellationToken);
    }
  }
}
