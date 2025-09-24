using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.MongoDB.Repositories.Contracts;
public interface IRepository<T>
{
  Task<T?> GetByIdAsync(string id);
  Task<IEnumerable<T>> GetAllAsync();
  Task AddAsync(T entity);
  Task UpdateAsync(T entity);
  Task DeleteAsync(string id);
}
