using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Handlers;
// A handler for a query that returns a response
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
  Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}

