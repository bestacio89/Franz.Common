#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

/// <summary>
/// Cosmos DB saga repository using tenant partitioning and EF Core ETag concurrency.
/// </summary>
public sealed class CosmosSagaRepository : ISagaRepository
{
  private readonly CosmosSagaDbContext _db;
  private readonly ISagaStateSerializer _serializer;

  public CosmosSagaRepository(CosmosSagaDbContext db, ISagaStateSerializer serializer)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
  }

  public async Task<SagaPersistenceRecord?> LoadAsync(
      Guid sagaId,
      string? tenantId,
      Type stateType,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(stateType);
    var partitionKey = tenantId ?? string.Empty;

    var document = await _db.SagaStates.SingleOrDefaultAsync(
        x => x.Id == sagaId && x.TenantId == partitionKey,
        cancellationToken).ConfigureAwait(false);

    if (document is null)
      return null;

    var targetType = stateType == typeof(object)
        ? Type.GetType(document.StateType, throwOnError: true)!
        : stateType;

    return new SagaPersistenceRecord
    {
      SagaId = document.Id,
      CorrelationId = document.CorrelationId,
      TenantId = document.TenantId,
      SagaType = document.SagaType,
      State = _serializer.Deserialize(document.Payload, targetType),
      Status = (SagaLifecycleStatus)document.Status,
      Version = document.Version,
      CreatedAt = document.CreatedAt,
      UpdatedAt = document.UpdatedAt,
      CompletedAt = document.CompletedAt,
      FailedAt = document.FailedAt,
      NextAttemptAt = document.NextAttemptAt,
      LastMessageId = document.LastMessageId,
      LastError = document.LastError
    };
  }

  public async Task<SagaPersistenceRecord> SaveAsync(
      SagaPersistenceRecord record,
      long? expectedVersion,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(record);
    if (record.SagaId == Guid.Empty)
      throw new ArgumentException("SagaId cannot be empty.", nameof(record));
    if (record.State is not ISagaState)
      throw new ArgumentException("Saga state must implement ISagaState.", nameof(record));

    var tenantId = record.TenantId ?? string.Empty;
    var now = DateTimeOffset.UtcNow;
    CosmosSagaStateDocument document;

    if (expectedVersion is null)
    {
      document = new CosmosSagaStateDocument(record.SagaId)
      {
        TenantId = tenantId,
        CorrelationId = record.CorrelationId,
        SagaType = record.SagaType,
        StateType = record.State.GetType().AssemblyQualifiedName!,
        Payload = _serializer.Serialize(record.State),
        Status = (int)record.Status,
        Version = 1,
        CreatedAt = record.CreatedAt,
        UpdatedAt = now,
        CompletedAt = record.CompletedAt,
        FailedAt = record.FailedAt,
        NextAttemptAt = record.NextAttemptAt,
        LastMessageId = record.LastMessageId,
        LastError = record.LastError
      };

      document.MarkCreated("saga-orchestrator");
      await _db.SagaStates.AddAsync(document, cancellationToken).ConfigureAwait(false);
    }
    else
    {
      document = await _db.SagaStates.SingleOrDefaultAsync(
          x => x.Id == record.SagaId && x.TenantId == tenantId,
          cancellationToken).ConfigureAwait(false)
          ?? throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      if (document.Version != expectedVersion.Value)
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      document.CorrelationId = record.CorrelationId;
      document.SagaType = record.SagaType;
      document.StateType = record.State.GetType().AssemblyQualifiedName!;
      document.Payload = _serializer.Serialize(record.State);
      document.Status = (int)record.Status;
      document.Version = checked(expectedVersion.Value + 1);
      document.UpdatedAt = now;
      document.CompletedAt = record.CompletedAt;
      document.FailedAt = record.FailedAt;
      document.NextAttemptAt = record.NextAttemptAt;
      document.LastMessageId = record.LastMessageId;
      document.LastError = record.LastError;
      document.MarkUpdated("saga-orchestrator");
    }

    try
    {
      await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException)
    {
      throw new SagaConcurrencyException(record.SagaId, expectedVersion);
    }
    catch (DbUpdateException) when (expectedVersion is null)
    {
      var exists = await _db.SagaStates.AsNoTracking().AnyAsync(
          x => x.Id == record.SagaId && x.TenantId == tenantId,
          cancellationToken).ConfigureAwait(false);

      if (exists)
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      throw;
    }

    return record with
    {
      TenantId = tenantId,
      Version = document.Version,
      UpdatedAt = document.UpdatedAt
    };
  }

  public async Task DeleteAsync(
      Guid sagaId,
      string? tenantId,
      long expectedVersion,
      CancellationToken cancellationToken = default)
  {
    var document = await _db.SagaStates.SingleOrDefaultAsync(
        x => x.Id == sagaId && x.TenantId == (tenantId ?? string.Empty),
        cancellationToken).ConfigureAwait(false)
        ?? throw new SagaConcurrencyException(sagaId, expectedVersion);

    if (document.Version != expectedVersion)
      throw new SagaConcurrencyException(sagaId, expectedVersion);

    _db.SagaStates.Remove(document);

    try
    {
      await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException)
    {
      throw new SagaConcurrencyException(sagaId, expectedVersion);
    }
  }
}
