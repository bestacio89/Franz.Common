namespace Franz.Common.AzureCosmosDB.Repositories;

/// <summary>Resource and request diagnostics returned by a native Cosmos item operation.</summary>
public sealed record CosmosItemResult<TDocument>(
    TDocument Resource,
    double RequestCharge,
    string? ETag,
    string? ActivityId);

/// <summary>Results and aggregate request diagnostics for a tenant-partition-scoped Cosmos query.</summary>
public sealed record CosmosQueryResult<TDocument>(
    IReadOnlyList<TDocument> Items,
    double RequestCharge,
    string? ContinuationToken);

/// <summary>Request diagnostics for a Cosmos operation that does not return a document.</summary>
public sealed record CosmosOperationMetadata(
    double RequestCharge,
    string? ETag,
    string? ActivityId);
