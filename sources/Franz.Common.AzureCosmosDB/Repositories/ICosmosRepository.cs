using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;
namespace Franz.Common.AzureCosmosDB.Repositories;

public interface ICosmosRepository<T>
    where T : class
{
  Task<T?> GetByIdAsync(string id, string partitionKey);
  Task<IEnumerable<T>> GetAllAsync();
  Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
  Task AddAsync(T entity, string partitionKey);
  Task UpdateAsync(string id, string partitionKey, T entity);
  Task DeleteAsync(string id, string partitionKey);
}
