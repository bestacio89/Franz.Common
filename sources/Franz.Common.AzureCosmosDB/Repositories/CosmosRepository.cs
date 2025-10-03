using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.AzureCosmosDB.Repositories;

public class CosmosRepository<T> : ICosmosRepository<T>
    where T : class
{
  private readonly Container _container;

  public CosmosRepository(Database database, string containerName, string partitionKeyPath)
  {
    _container = database.CreateContainerIfNotExistsAsync(
        containerName, partitionKeyPath).GetAwaiter().GetResult();
  }

  public async Task<T?> GetByIdAsync(string id, string partitionKey)
  {
    try
    {
      var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
      return response.Resource;
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      return null;
    }
  }

  public async Task<IEnumerable<T>> GetAllAsync()
  {
    var query = _container.GetItemQueryIterator<T>("SELECT * FROM c");
    var results = new List<T>();
    while (query.HasMoreResults)
    {
      var response = await query.ReadNextAsync();
      results.AddRange(response);
    }
    return results;
  }

  public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
  {
    // For simplicity: run LINQ-to-Cosmos translation
    var query = _container.GetItemLinqQueryable<T>()
                          .Where(predicate)
                          .ToFeedIterator();

    var results = new List<T>();
    while (query.HasMoreResults)
    {
      var response = await query.ReadNextAsync();
      results.AddRange(response);
    }
    return results;
  }

  public async Task AddAsync(T entity, string partitionKey) =>
      await _container.CreateItemAsync(entity, new PartitionKey(partitionKey));

  public async Task UpdateAsync(string id, string partitionKey, T entity) =>
      await _container.ReplaceItemAsync(entity, id, new PartitionKey(partitionKey));

  public async Task DeleteAsync(string id, string partitionKey) =>
      await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
}
