using Franz.Common.MultiTenancy;
using Microsoft.Azure.Cosmos;

namespace Franz.Common.AzureCosmosDB.Repositories;

/// <summary>
/// Native Azure Cosmos DB repository. Every point read, query, write and delete is
/// constrained by the validated tenant partition key supplied to the Cosmos SDK.
/// </summary>
public sealed class CosmosTenantRepository<TDocument>
    where TDocument : class, ICosmosTenantDocument
{
    private readonly Container _container;
    private readonly ITenantExecutionContextAccessor _tenantContextAccessor;

    public CosmosTenantRepository(
        Container container,
        ITenantExecutionContextAccessor tenantContextAccessor)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _tenantContextAccessor = tenantContextAccessor
            ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    public async Task<TDocument> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
        => (await GetByIdWithMetadataAsync(id, cancellationToken).ConfigureAwait(false)).Resource;

    public async Task<CosmosItemResult<TDocument>> GetByIdWithMetadataAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var tenantId = GetRequiredTenantId();

        try
        {
            var response = await _container.ReadItemAsync<TDocument>(
                id,
                GetPartitionKey(tenantId),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return new CosmosItemResult<TDocument>(
                response.Resource,
                response.RequestCharge,
                response.ETag,
                response.ActivityId);
        }
        catch (CosmosException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException(
                $"Tenant-owned Cosmos document '{id}' was not found for tenant '{tenantId:D}'.",
                exception);
        }
    }

    public async Task<IReadOnlyList<TDocument>> QueryAsync(
        QueryDefinition query,
        CancellationToken cancellationToken = default)
        => (await QueryWithMetadataAsync(query, cancellationToken).ConfigureAwait(false)).Items;

    /// <summary>
    /// Executes a caller-defined Cosmos SQL query inside the validated tenant partition.
    /// The SDK partition-key option prevents the query from crossing tenant partitions.
    /// </summary>
    public async Task<CosmosQueryResult<TDocument>> QueryWithMetadataAsync(
        QueryDefinition query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var tenantId = GetRequiredTenantId();
        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = GetPartitionKey(tenantId)
        };

        using var iterator = _container.GetItemQueryIterator<TDocument>(
            query,
            requestOptions: requestOptions);

        var documents = new List<TDocument>();
        var requestCharge = 0d;
        string? continuationToken = null;

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            documents.AddRange(page);
            requestCharge += page.RequestCharge;
            continuationToken = page.ContinuationToken;
        }

        return new CosmosQueryResult<TDocument>(documents, requestCharge, continuationToken);
    }

    public async Task<CosmosItemResult<TDocument>> CreateAsync(
        TDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(document.Id);
        var tenantId = GetRequiredTenantId();
        EnsureTenantOwnership(document, tenantId);

        var response = await _container.CreateItemAsync(
            document,
            GetPartitionKey(tenantId),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new CosmosItemResult<TDocument>(
            response.Resource,
            response.RequestCharge,
            response.ETag,
            response.ActivityId);
    }

    /// <summary>
    /// Replaces a document using Cosmos ETag optimistic concurrency.
    /// A stale ETag is rejected by Cosmos with HTTP 412 (Precondition Failed).
    /// </summary>
    public async Task<CosmosItemResult<TDocument>> ReplaceAsync(
        TDocument document,
        string ifMatchEtag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(document.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ifMatchEtag);

        var tenantId = GetRequiredTenantId();
        EnsureTenantOwnership(document, tenantId);

        var response = await _container.ReplaceItemAsync(
            document,
            document.Id,
            GetPartitionKey(tenantId),
            new ItemRequestOptions { IfMatchEtag = ifMatchEtag },
            cancellationToken).ConfigureAwait(false);

        return new CosmosItemResult<TDocument>(
            response.Resource,
            response.RequestCharge,
            response.ETag,
            response.ActivityId);
    }

    public async Task<CosmosOperationMetadata> DeleteAsync(
        string id,
        string? ifMatchEtag = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var tenantId = GetRequiredTenantId();

        var response = await _container.DeleteItemAsync<TDocument>(
            id,
            GetPartitionKey(tenantId),
            new ItemRequestOptions { IfMatchEtag = ifMatchEtag },
            cancellationToken).ConfigureAwait(false);

        return new CosmosOperationMetadata(
            response.RequestCharge,
            response.ETag,
            response.ActivityId);
    }

    public async Task<IReadOnlyList<TDocument>> ReadManyAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
        => (await ReadManyWithMetadataAsync(ids, cancellationToken).ConfigureAwait(false)).Items;

    public async Task<CosmosQueryResult<TDocument>> ReadManyWithMetadataAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var tenantId = GetRequiredTenantId();
        var partitionKey = GetPartitionKey(tenantId);
        var distinctIds = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (distinctIds.Length == 0)
            return new CosmosQueryResult<TDocument>(Array.Empty<TDocument>(), 0, null);

        var items = distinctIds
            .Select(id => (id, partitionKey))
            .ToList();

        var response = await _container.ReadManyItemsAsync<TDocument>(
            items,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new CosmosQueryResult<TDocument>(
            response.ToList(),
            response.RequestCharge,
            null);
    }

    private Guid GetRequiredTenantId()
    {
        var tenantId = _tenantContextAccessor.Current?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
            throw new TenantIsolationException(
                "Cosmos DB tenant operations require a validated tenant execution context.");

        return tenantId.Value;
    }

    private static void EnsureTenantOwnership(TDocument document, Guid tenantId)
    {
        if (document.TenantId == Guid.Empty)
            document.TenantId = tenantId;
        else if (document.TenantId != tenantId)
            throw new TenantIsolationException(
                "A Cosmos document cannot be written outside its owning tenant partition.");
    }

    private static PartitionKey GetPartitionKey(Guid tenantId)
        => new(tenantId.ToString("D"));
}
