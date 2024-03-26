using Franz.Common.Business.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Franz.Common.EntityFramework.Behaviors;
public class PersistenceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICommandBaseRequest
{
  private readonly DbContextBase dbContextBase;
  private readonly ILogger<PersistenceBehavior<TRequest, TResponse>> logger;

  public PersistenceBehavior(DbContextBase dbContextBase, ILogger<PersistenceBehavior<TRequest, TResponse>> logger)
  {
    this.dbContextBase = dbContextBase;
    this.logger = logger;
  }

  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
  {
    logger.LogInformation($"Persistence handling {typeof(TRequest).Name}");

    var response = await next();

    logger.LogInformation($"Persistence handled {typeof(TResponse).Name}");

    await dbContextBase.SaveEntitiesAsync(cancellationToken);

    return response;
  }
}
