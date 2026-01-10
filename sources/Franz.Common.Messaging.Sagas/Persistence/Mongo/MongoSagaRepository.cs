#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

/// <summary>
/// A MongoDB-backed implementation of ISagaRepository.
/// Stores saga states as JSON + metadata, and supports polymorphic saga state types.
/// </summary>
public sealed class MongoSagaRepository : ISagaRepository
{
  private readonly IMongoCollection<SagaStateDocument> _collection;
  private readonly JsonSagaStateSerializer _serializer;

  public MongoSagaRepository(
      IMongoDatabase database,
      JsonSagaStateSerializer serializer)
  {
    _collection = database.GetCollection<SagaStateDocument>("sagaStates");
    _serializer = serializer;
  }

  public async Task<object?> LoadStateAsync(
      string sagaId,
      Type stateType,
      CancellationToken cancellationToken)
  {
    var filter = Builders<SagaStateDocument>.Filter.Eq(x => x.Id, sagaId);

    var doc = await _collection
      .Find(filter)
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

    // Serialize state to JSON
    string json = _serializer.Serialize(state);

    string? concurrency =
      state is ISagaState s ? s.ConcurrencyToken : null;

    var filter = Builders<SagaStateDocument>.Filter.Eq(x => x.Id, sagaId);

    if (concurrency != null)
    {
      // Optimistic concurrency check
      filter &= Builders<SagaStateDocument>.Filter.Eq(x => x.ConcurrencyToken, concurrency);
    }

    var update = Builders<SagaStateDocument>.Update
      .Set(x => x.Id, sagaId)
      .Set(x => x.StateType, state.GetType().AssemblyQualifiedName!)
      .Set(x => x.Payload, json)
      .Set(x => x.UpdatedAt, DateTime.UtcNow)
      .Set(x => x.ConcurrencyToken, Guid.NewGuid().ToString("N")); // auto new token on write

    var options = new UpdateOptions { IsUpsert = concurrency == null };

    var result = await _collection.UpdateOneAsync(filter, update, options, cancellationToken);

    if (result.MatchedCount == 0 && concurrency != null)
      throw new InvalidOperationException(
        $"Concurrency violation while saving saga '{sagaId}'.");
  }

  public async Task DeleteStateAsync(
      string sagaId,
      CancellationToken cancellationToken)
  {
    var filter = Builders<SagaStateDocument>.Filter.Eq(x => x.Id, sagaId);
    await _collection.DeleteOneAsync(filter, cancellationToken);
  }

  /// <summary>
  /// Internal MongoDB document shape.
  /// </summary>
  private sealed class SagaStateDocument
  {
    [BsonId]
    public string Id { get; set; } = default!;

    public string StateType { get; set; } = default!;

    public string Payload { get; set; } = default!;

    public string? ConcurrencyToken { get; set; }

    public DateTime UpdatedAt { get; set; }
  }
}
