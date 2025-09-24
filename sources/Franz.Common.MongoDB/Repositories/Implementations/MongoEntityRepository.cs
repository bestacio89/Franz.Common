using Franz.Common.Errors;
using MongoDB.Driver;

/// <summary>
/// Generic repository providing CRUD operations for MongoDB entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class MongoRepository<TEntity> where TEntity : IEntity
{
  protected readonly IMongoCollection<TEntity> Collection;

  public MongoRepository(IMongoDatabase database, string collectionName
    )
  {
    if (database == null) throw new ArgumentNullException(nameof(database));

    // Default collection name = entity type name
    collectionName ??= typeof(TEntity).Name;
    Collection = database.GetCollection<TEntity>(collectionName);
  }

  /// <summary>
  /// Retrieves an entity by ID or throws a NotFoundException if not found.
  /// </summary>
  public async Task<TEntity> GetByIdAsync(string id, CancellationToken cancellationToken = default)
  {
    var filter = Builders<TEntity>.Filter.Eq(e => e.Id, id);
    var entity = await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

    return entity ?? throw new NotFoundException(
        $"Entity {typeof(TEntity).Name} with ID {id} not found.");
  }

  /// <summary>
  /// Adds a new entity.
  /// </summary>
  public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
  }

  /// <summary>
  /// Updates an existing entity (replaces entire document).
  /// </summary>
  public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
    var result = await Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);

    if (result.MatchedCount == 0)
      throw new NotFoundException(
          $"Entity {typeof(TEntity).Name} with ID {entity.Id} not found for update.");
  }

  /// <summary>
  /// Deletes an entity.
  /// </summary>
  public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
    var result = await Collection.DeleteOneAsync(filter, cancellationToken);

    if (result.DeletedCount == 0)
      throw new NotFoundException(
          $"Entity {typeof(TEntity).Name} with ID {entity.Id} not found for deletion.");
  }
}

/// <summary>
/// Base interface for MongoDB entities (so we can require an Id).
/// </summary>
public interface IEntity
{
  string Id { get; set; }
}