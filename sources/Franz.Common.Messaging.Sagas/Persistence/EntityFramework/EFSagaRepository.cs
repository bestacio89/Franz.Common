#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

/// <summary>
/// Relational saga repository with tenant-scoped keys and database-enforced optimistic concurrency.
/// </summary>
public sealed class EFSagaRepository : ISagaRepository
{
  private readonly SagaDbContext _db;
  private readonly ISagaStateSerializer _serializer;

  public EFSagaRepository(SagaDbContext db, ISagaStateSerializer serializer)
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
    tenantId = SagaTenantId.Normalize(tenantId);
    var scopedTenantId = tenantId ?? string.Empty;

    var entity = await _db.SagaStates
        .AsNoTracking()
        .SingleOrDefaultAsync(
            x => x.SagaId == sagaId && x.TenantId == scopedTenantId,
            cancellationToken)
        .ConfigureAwait(false);

    if (entity is null)
      return null;

    var targetType = stateType == typeof(object)
        ? Type.GetType(entity.StateType, throwOnError: true)!
        : stateType;

    return new SagaPersistenceRecord
    {
      SagaId = entity.SagaId,
      CorrelationId = entity.CorrelationId,
      TenantId = entity.TenantId.Length == 0 ? null : entity.TenantId,
      SagaType = entity.SagaType,
      State = _serializer.Deserialize(entity.SerializedState, targetType),
      Status = (SagaLifecycleStatus)entity.Status,
      Version = entity.Version,
      CreatedAt = entity.CreatedAt,
      UpdatedAt = entity.UpdatedAt,
      CompletedAt = entity.CompletedAt,
      FailedAt = entity.FailedAt,
      NextAttemptAt = entity.NextAttemptAt,
      LastMessageId = entity.LastMessageId,
      LastError = entity.LastError
    };
  }

  public async Task<SagaPersistenceRecord> SaveAsync(
      SagaPersistenceRecord record,
      long? expectedVersion,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(record);
    var tenantId = SagaTenantId.Normalize(record.TenantId);
    if (record.SagaId == Guid.Empty)
      throw new ArgumentException("SagaId cannot be empty.", nameof(record));
    if (record.State is not ISagaState)
      throw new ArgumentException("Saga state must implement ISagaState.", nameof(record));

    var now = DateTimeOffset.UtcNow;
    var scopedTenantId = tenantId ?? string.Empty;
    SagaStateEntity entity;

    if (expectedVersion is null)
    {
      entity = new SagaStateEntity
      {
        SagaId = record.SagaId,
        CorrelationId = record.CorrelationId,
        TenantId = scopedTenantId,
        SagaType = record.SagaType,
        StateType = record.State.GetType().AssemblyQualifiedName!,
        SerializedState = _serializer.Serialize(record.State),
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

      _db.SagaStates.Add(entity);
    }
    else
    {
      entity = await _db.SagaStates.SingleOrDefaultAsync(
          x => x.SagaId == record.SagaId && x.TenantId == scopedTenantId,
          cancellationToken).ConfigureAwait(false)
          ?? throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      if (entity.Version != expectedVersion.Value)
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      entity.CorrelationId = record.CorrelationId;
      entity.SagaType = record.SagaType;
      entity.StateType = record.State.GetType().AssemblyQualifiedName!;
      entity.SerializedState = _serializer.Serialize(record.State);
      entity.Status = (int)record.Status;
      entity.Version = checked(expectedVersion.Value + 1);
      entity.UpdatedAt = now;
      entity.CompletedAt = record.CompletedAt;
      entity.FailedAt = record.FailedAt;
      entity.NextAttemptAt = record.NextAttemptAt;
      entity.LastMessageId = record.LastMessageId;
      entity.LastError = record.LastError;
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
          x => x.SagaId == record.SagaId && x.TenantId == scopedTenantId,
          cancellationToken).ConfigureAwait(false);

      if (exists)
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      throw;
    }

    return record with
    {
      TenantId = tenantId,
      Version = entity.Version,
      UpdatedAt = entity.UpdatedAt
    };
  }

  public async Task DeleteAsync(
      Guid sagaId,
      string? tenantId,
      long expectedVersion,
      CancellationToken cancellationToken = default)
  {
    tenantId = SagaTenantId.Normalize(tenantId);
    var entity = await _db.SagaStates.SingleOrDefaultAsync(
        x => x.SagaId == sagaId && x.TenantId == (tenantId ?? string.Empty),
        cancellationToken).ConfigureAwait(false)
        ?? throw new SagaConcurrencyException(sagaId, expectedVersion);

    if (entity.Version != expectedVersion)
      throw new SagaConcurrencyException(sagaId, expectedVersion);

    _db.SagaStates.Remove(entity);

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
