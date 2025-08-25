using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines;
public interface IPipeline<TRequest, TResponse>
{
  Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default);
}
