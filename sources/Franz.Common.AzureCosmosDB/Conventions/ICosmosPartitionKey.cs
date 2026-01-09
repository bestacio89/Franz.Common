namespace Franz.Common.AzureCosmosDB.Conventions;

/// <summary>
/// Marker interface indicating that the entity:
///  - MUST define a PartitionKey property
///  - MUST be mapped to a Cosmos container
/// The PartitionKey property will be used automatically during model configuration.
/// </summary>
public interface ICosmosPartitionKey
{
}
