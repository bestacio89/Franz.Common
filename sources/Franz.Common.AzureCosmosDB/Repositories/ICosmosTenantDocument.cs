namespace Franz.Common.AzureCosmosDB.Repositories;

/// <summary>
/// Native Cosmos DB document contract for tenant-owned documents.
/// The container must use /tenantId (lower camel-case) as its partition-key path.
/// </summary>
public interface ICosmosTenantDocument
{
    string Id { get; set; }
    Guid TenantId { get; set; }
}
