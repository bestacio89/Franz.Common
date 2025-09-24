using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.MongoDB.Repositories.Contracts;
public interface IEventRepository<T> : IRepository<T> 
  where T : IEvent 
{
}
