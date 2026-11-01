using System.Linq.Expressions;
using Franz.Common.MultiTenancy;
using Franz.Common.MongoDB.Repositories.Contracts;
using MongoDB.Driver;

namespace Franz.Common.MongoDB.Repositories.Implementations;

/// <summary>
/// Tenant-scoped MongoDB repository. Every read and mutation includes the validated
/// execution tenant in its server-side MongoDB filter.
/// </summary>
public sealed class MongoTenantRepository<TEntity>
    where TEntity : class, IMongoTenantEntity
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<TEntity> _collection;
    private readonly ITenantExecutionContextAccessor _tenantContextAccessor;

    public MongoTenantRepository(
        IMongoDatabase database,
        ITenantExecutionContextAccessor tenantContextAccessor,
        string collectionName)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(tenantContextAccessor);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        _database = database;
        _collection = database.GetCollection<TEntity>(collectionName);
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<TEntity> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var filter = TenantFilter() & Builders<TEntity>.Filter.Eq(entity => entity.Id, id);
        var entity = await Find(filter, session)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entity ?? throw new Franz.Common.Errors.NotFoundException(
            $"Tenant-owned entity {typeof(TEntity).Name} with ID '{id}' was not found.");
    }

    public Task<List<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
        => Find(TenantFilter(), session)
            .ToListAsync(cancellationToken);

    public Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Find(TenantFilter() & Builders<TEntity>.Filter.Where(predicate), session)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Id);
        var tenantId = GetRequiredTenantId();
        EnsureTenantOwnership(entity, tenantId);

        if (session is null)
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken).ConfigureAwait(false);
        else
            await _collection.InsertOneAsync(session, entity, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var documents = entities.ToArray();
        if (documents.Length == 0)
            return;

        var tenantId = GetRequiredTenantId();
        foreach (var entity in documents)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentException.ThrowIfNullOrWhiteSpace(entity.Id);
            EnsureTenantOwnership(entity, tenantId);
        }

        var options = new InsertManyOptions { IsOrdered = true };
        if (session is null)
            await _collection.InsertManyAsync(documents, options, cancellationToken).ConfigureAwait(false);
        else
            await _collection.InsertManyAsync(session, documents, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Id);
        var tenantId = GetRequiredTenantId();
        EnsureExistingOwnership(entity, tenantId);

        var filter = TenantFilter() & Builders<TEntity>.Filter.Eq(document => document.Id, entity.Id);
        var options = new ReplaceOptions { IsUpsert = false };
        var result = session is null
            ? await _collection.ReplaceOneAsync(filter, entity, options, cancellationToken).ConfigureAwait(false)
            : await _collection.ReplaceOneAsync(session, filter, entity, options, cancellationToken).ConfigureAwait(false);

        if (result.MatchedCount == 0)
            throw new Franz.Common.Errors.NotFoundException(
                $"Tenant-owned entity {typeof(TEntity).Name} with ID '{entity.Id}' was not found for update.");
    }

    public async Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Id);
        var tenantId = GetRequiredTenantId();
        EnsureExistingOwnership(entity, tenantId);

        var filter = TenantFilter() & Builders<TEntity>.Filter.Eq(document => document.Id, entity.Id);
        var result = session is null
            ? await _collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false)
            : await _collection.DeleteOneAsync(session, filter, cancellationToken).ConfigureAwait(false);

        if (result.DeletedCount == 0)
            throw new Franz.Common.Errors.NotFoundException(
                $"Tenant-owned entity {typeof(TEntity).Name} with ID '{entity.Id}' was not found for deletion.");
    }

    /// <summary>
    /// Performs an atomic find-and-update constrained to the current tenant and document ID.
    /// The update builder rejects all attempts to modify TenantId.
    /// </summary>
    public async Task<TEntity> UpdateOneAsync(
        string id,
        Action<MongoTenantUpdateBuilder<TEntity>> configure,
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(configure);

        var update = BuildUpdate(configure);
        var filter = TenantFilter() & Builders<TEntity>.Filter.Eq(document => document.Id, id);
        var options = new FindOneAndUpdateOptions<TEntity>
        {
            ReturnDocument = ReturnDocument.After,
            IsUpsert = false
        };

        var updated = session is null
            ? await _collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken).ConfigureAwait(false)
            : await _collection.FindOneAndUpdateAsync(session, filter, update, options, cancellationToken).ConfigureAwait(false);

        return updated ?? throw new Franz.Common.Errors.NotFoundException(
            $"Tenant-owned entity {typeof(TEntity).Name} with ID '{id}' was not found for atomic update.");
    }

    /// <summary>
    /// Performs a tenant-filtered atomic update across matching documents.
    /// </summary>
    public Task<UpdateResult> UpdateManyAsync(
        Expression<Func<TEntity, bool>> predicate,
        Action<MongoTenantUpdateBuilder<TEntity>> configure,
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(configure);

        var filter = TenantFilter() & Builders<TEntity>.Filter.Where(predicate);
        var update = BuildUpdate(configure);

        return session is null
            ? _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken)
            : _collection.UpdateManyAsync(session, filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes a native MongoDB bulk write. Every update model is scoped to the current tenant.
    /// All update definitions are validated before the bulk operation is sent.
    /// </summary>
    public async Task<BulkWriteResult<TEntity>> BulkUpdateAsync(
        IEnumerable<MongoTenantBulkUpdate<TEntity>> updates,
        bool ordered = true,
        CancellationToken cancellationToken = default,
        IClientSessionHandle? session = null)
    {
        ArgumentNullException.ThrowIfNull(updates);
        var tenantId = GetRequiredTenantId();
        var operations = updates.ToArray();
        if (operations.Length == 0)
            throw new ArgumentException("At least one MongoDB bulk update is required.", nameof(updates));

        var writeModels = new List<WriteModel<TEntity>>(operations.Length);
        foreach (var operation in operations)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ArgumentException.ThrowIfNullOrWhiteSpace(operation.Id);
            ArgumentNullException.ThrowIfNull(operation.Configure);

            var update = BuildUpdate(operation.Configure);
            var filter = Builders<TEntity>.Filter.Eq(entity => entity.TenantId, tenantId)
                & Builders<TEntity>.Filter.Eq(entity => entity.Id, operation.Id);

            writeModels.Add(new UpdateOneModel<TEntity>(filter, update));
        }

        var options = new BulkWriteOptions { IsOrdered = ordered };
        return session is null
            ? await _collection.BulkWriteAsync(writeModels, options, cancellationToken).ConfigureAwait(false)
            : await _collection.BulkWriteAsync(session, writeModels, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs repository operations in a MongoDB transaction.
    /// The MongoDB deployment must be a replica set or sharded cluster.
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IClientSessionHandle, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default,
        TransactionOptions? transactionOptions = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var tenantId = GetRequiredTenantId();
        using var session = await _database.Client
            .StartSessionAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return await session.WithTransactionAsync(
            async (transactionSession, transactionCancellationToken) =>
            {
                if (GetRequiredTenantId() != tenantId)
                    throw new TenantIsolationException(
                        "The tenant execution context cannot change during a MongoDB transaction.");

                return await operation(transactionSession, transactionCancellationToken)
                    .ConfigureAwait(false);
            },
            transactionOptions,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the native compound unique index required to make document identity unique per tenant.
    /// Call during deployment/index migration, not on every request.
    /// </summary>
    public Task CreateTenantIdentityIndexAsync(CancellationToken cancellationToken = default)
    {
        var keys = Builders<TEntity>.IndexKeys
            .Ascending(entity => entity.TenantId)
            .Ascending(entity => entity.Id);
        var model = new CreateIndexModel<TEntity>(
            keys,
            new CreateIndexOptions { Name = "ix_tenant_id" });

        return _collection.Indexes.CreateOneAsync(
            model,
            cancellationToken: cancellationToken);
    }

    private IFindFluent<TEntity, TEntity> Find(
        FilterDefinition<TEntity> filter,
        IClientSessionHandle? session)
        => session is null
            ? _collection.Find(filter)
            : _collection.Find(session, filter);

    private static UpdateDefinition<TEntity> BuildUpdate(
        Action<MongoTenantUpdateBuilder<TEntity>> configure)
    {
        var builder = new MongoTenantUpdateBuilder<TEntity>();
        configure(builder);
        return builder.Build();
    }

    private FilterDefinition<TEntity> TenantFilter()
        => Builders<TEntity>.Filter.Eq(entity => entity.TenantId, GetRequiredTenantId());

    private Guid GetRequiredTenantId()
    {
        var tenantId = _tenantContextAccessor.Current?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
            throw new TenantIsolationException(
                "MongoDB tenant operations require a validated tenant execution context.");

        return tenantId.Value;
    }

    private static void EnsureTenantOwnership(TEntity entity, Guid tenantId)
    {
        if (entity.TenantId == Guid.Empty)
            entity.TenantId = tenantId;
        else
            EnsureExistingOwnership(entity, tenantId);
    }

    private static void EnsureExistingOwnership(TEntity entity, Guid tenantId)
    {
        if (entity.TenantId != tenantId)
            throw new TenantIsolationException(
                "A MongoDB document cannot be updated or deleted outside its owning tenant.");
    }
}
