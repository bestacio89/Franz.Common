using Microsoft.Azure.Cosmos;

namespace Franz.Common.AzureCosmosDB
{
  /// <summary>
  /// Base class for Cosmos DB contexts in Franz.
  /// Provides a strongly-typed entry point for generic repositories and services.
  /// </summary>
  public abstract class AzureCosmosStore
  {
    protected readonly CosmosClient _client;
    protected readonly Database _database;

    protected AzureCosmosStore(CosmosClient client, string databaseName)
    {
      _client = client ?? throw new ArgumentNullException(nameof(client));
      _database = _client.GetDatabase(databaseName);
    }

    /// <summary>
    /// Gets the underlying CosmosClient for advanced operations.
    /// </summary>
    public CosmosClient Client => _client;

    /// <summary>
    /// Gets the database instance for this store.
    /// </summary>
    public Database Database => _database;

    /// <summary>
    /// Get a container reference by name.
    /// </summary>
    public Container GetContainer(string containerName) => _database.GetContainer(containerName);
  }
}
