#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

public sealed class CosmosSagaRepository : ISagaRepository
{
  private readonly CosmosSagaDbContext _db;
  private readonly JsonSagaStateSerializer _serializer;

  public CosmosSagaRepository(
      CosmosSagaDbContext db,
      JsonSagaStateSerializer serializer)
  {
    _db = db;
    _serializer = serializer;
  }

  public async Task<object?> LoadStateAsync(
      string sagaId,
      Type stateType,
      CancellationToken cancellationToken)
  {
    var doc = await _db.SagaStates
        .Where(x => x.Id == sagaId)
        .FirstOrDefaultAsync(cancellationToken);

    if (doc == null)
      return null;

    return _serializer.Deserialize(doc.Payload, stateType);
  }

  public async Task SaveStateAsync(
      string sagaId,
      object state,
      CancellationToken cancellationToken)
  {
    if (state is not ISagaState)
      throw new InvalidOperationException("State must implement ISagaState.");

    string json = _serializer.Serialize(state);

    string? concurrency =
        state is ISagaState s ? s.ConcurrencyToken : null;

    var existing = await _db.SagaStates
        .Where(x => x.Id == sagaId)
        .FirstOrDefaultAsync(cancellationToken);

    if (existing == null)
    {
      var doc = new CosmosSagaStateDocument(sagaId)
      {
        
        Payload = json,
        SagaType = state.GetType().AssemblyQualifiedName!,
        ConcurrencyToken = Guid.NewGuid().ToString("N"),
        UpdatedAt = DateTime.UtcNow
      };

      // Audit lifecycle
      doc.MarkCreated("saga-orchestrator");

      await _db.SagaStates.AddAsync(doc, cancellationToken);
    }
    else
    {
      if (concurrency != null && existing.ConcurrencyToken != concurrency)
        throw new InvalidOperationException(
            $"Concurrency conflict saving saga '{sagaId}'.");

      existing.Payload = json;
      existing.UpdatedAt = DateTime.UtcNow;
      existing.ConcurrencyToken = Guid.NewGuid().ToString("N");

      // Audit lifecycle
      existing.MarkUpdated("saga-orchestrator");
    }

    await _db.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteStateAsync(
      string sagaId,
      CancellationToken cancellationToken)
  {
    var doc = await _db.SagaStates
        .Where(x => x.Id == sagaId)
        .FirstOrDefaultAsync(cancellationToken);

    if (doc != null)
    {
      _db.SagaStates.Remove(doc);
      await _db.SaveChangesAsync(cancellationToken);
    }
  }
}
