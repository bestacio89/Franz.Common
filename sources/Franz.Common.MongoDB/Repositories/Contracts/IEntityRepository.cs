using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.MongoDB.Repositories.Contracts;
public interface IEntityRepository<T> : IRepository<T> 
  where T : class, IEntity
{
}
