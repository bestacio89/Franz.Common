using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Messages
{
   public interface IStreamQueryHandler<in TQuery, out TResponse>
      where TQuery : IStreamQuery<TResponse>
  {
    IAsyncEnumerable<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
  }
}
