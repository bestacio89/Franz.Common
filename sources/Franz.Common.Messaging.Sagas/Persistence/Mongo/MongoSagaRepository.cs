#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

/// <summary>
/// MongoDB saga repository using tenant-scoped document keys and atomic version-filtered updates.
/// </summary>
public sealed class MongoSagaRepository : ISagaRepository
{
  private readonly IMongoCollection<SagaStateDocument> _collection;
  private readonly ISagaStateSerializer _serializer;

  public MongoSagaRepository(IMongoDatabase database, ISagaStateSerializer serializer)
  {
    ArgumentNullException.ThrowIfNull(database);
    _collection = database.GetCollection<SagaStateDocument>("sagaStates");
    _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
  }

  public async Task<SagaPersistenceRecord?> LoadAsync(
      Guid sagaId,
      string? tenantId,
      Type stateType,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(stateType);
    var document = await _collection.Find(
        x => x.SagaId == sagaId && x.TenantId == tenantId)
        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    if (document is null)
      return null;

    var targetType = stateType == typeof(object)
        ? Type.GetType(document.StateType, throwOnError: true)!
        : stateType;

    return new SagaPersistenceRecord
    {
      SagaId = document.SagaId,
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

    var now = DateTimeOffset.UtcNow;
    var nextVersion = expectedVersion is null ? 1 : checked(expectedVersion.Value + 1);
    var payload = _serializer.Serialize(record.State);
    var stateType = record.State.GetType().AssemblyQualifiedName!;

    if (expectedVersion is null)
    {
      var document = new SagaStateDocument
      {
        Id = CreateDocumentId(record.TenantId, record.SagaId),
        SagaId = record.SagaId,
        TenantId = record.TenantId,
        SagaType = record.SagaType,
        StateType = stateType,
        Payload = payload,
        Status = (int)record.Status,
        Version = nextVersion,
        CreatedAt = record.CreatedAt,
        UpdatedAt = now,
        CompletedAt = record.CompletedAt,
        FailedAt = record.FailedAt,
        NextAttemptAt = record.NextAttemptAt,
        LastMessageId = record.LastMessageId,
        LastError = record.LastError
      };

      try
      {
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
      }
      catch (MongoWriteException exception) when (
          exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
      {
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);
      }
    }
    else
    {
      var filter = Builders<SagaStateDocument>.Filter.Eq(x => x.Id, CreateDocumentId(record.TenantId, record.SagaId)) &
                   Builders<SagaStateDocument>.Filter.Eq(x => x.Version, expectedVersion.Value);

      var update = Builders<SagaStateDocument>.Update
          .Set(x => x.SagaType, record.SagaType)
          .Set(x => x.StateType, stateType)
          .Set(x => x.Payload, payload)
          .Set(x => x.Status, (int)record.Status)
          .Set(x => x.Version, nextVersion)
          .Set(x => x.UpdatedAt, now)
          .Set(x => x.CompletedAt, record.CompletedAt)
          .Set(x => x.FailedAt, record.FailedAt)
          .Set(x => x.NextAttemptAt, record.NextAttemptAt)
          .Set(x => x.LastMessageId, record.LastMessageId)
          .Set(x => x.LastError, record.LastError);

      var result = await _collection.UpdateOneAsync(
          filter,
          update,
          cancellationToken: cancellationToken).ConfigureAwait(false);

      if (result.MatchedCount != 1)
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);
    }

    return record with { Version = nextVersion, UpdatedAt = now };
  }

  public async Task DeleteAsync(
      Guid sagaId,
      string? tenantId,
      long expectedVersion,
      CancellationToken cancellationToken = default)
  {
    var filter = Builders<SagaStateDocument>.Filter.Eq(x => x.Id, CreateDocumentId(tenantId, sagaId)) &
                 Builders<SagaStateDocument>.Filter.Eq(x => x.Version, expectedVersion);

    var result = await _collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
    if (result.DeletedCount != 1)
      throw new SagaConcurrencyException(sagaId, expectedVersion);
  }

  private static string CreateDocumentId(string? tenantId, Guid sagaId)
      => $"{tenantId ?? string.Empty}:{sagaId:N}";

  private sealed class SagaStateDocument
  {
    [BsonId]
    public string Id { get; set; } = default!;
    public Guid SagaId { get; set; }
    public string? TenantId { get; set; }
    public string SagaType { get; set; } = default!;
    public string StateType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public int Status { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public Guid? LastMessageId { get; set; }
    public string? LastError { get; set; }
  }
}
