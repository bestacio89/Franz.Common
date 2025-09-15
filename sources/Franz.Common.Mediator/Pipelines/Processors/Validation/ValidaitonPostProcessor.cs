using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Processors.Validation;
public class AuditPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
{
  public Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken = default)
  {
    Console.WriteLine($"[Audit] {typeof(TRequest).Name} -> {response}");
    return Task.CompletedTask;
  }
}
