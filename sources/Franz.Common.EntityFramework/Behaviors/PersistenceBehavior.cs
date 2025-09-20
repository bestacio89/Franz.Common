
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.EntityFramework.Behaviors
{
  public class PersistenceBehavior<TRequest, TResponse> : IPipeline<TRequest, TResponse>
      where TRequest : ICommand<TResponse>
  {
    private readonly DbContextBase _dbContextBase;
    private readonly ILogger<PersistenceBehavior<TRequest, TResponse>> _logger;

    public PersistenceBehavior(
        DbContextBase dbContextBase,
        ILogger<PersistenceBehavior<TRequest, TResponse>> logger)
    {
      _dbContextBase = dbContextBase;
      _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
      _logger.LogInformation($"Persistence handling {typeof(TRequest).Name}");

      var response = await next();

      _logger.LogInformation($"Persistence handled {typeof(TResponse).Name}");

      await _dbContextBase.SaveChangesAsync(cancellationToken);

      return response;
    }
  }
}
