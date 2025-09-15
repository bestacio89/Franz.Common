using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Transaction
{
  public class TransactionPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
    where TRequest : notnull
  {
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransactionOptions _options;

    public TransactionPipeline(IUnitOfWork unitOfWork, TransactionOptions options)
    {
      _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
      _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
      await _unitOfWork.BeginAsync(cancellationToken);

      try
      {
        var response = await next();
        await _unitOfWork.CommitAsync(cancellationToken);
        return response;
      }
      catch (Exception ex)
      {
        if (ShouldRollback(ex))
        {
          await _unitOfWork.RollbackAsync(cancellationToken);
        }

        throw;
      }
    }

    private bool ShouldRollback(Exception ex)
    {
      if (_options.RollbackOnAnyException)
        return true;

      if (_options.RollbackCondition != null && _options.RollbackCondition(ex))
        return true;

      if (_options.RollbackOnExceptions.Contains(ex.GetType()))
        return true;

      return false;
    }
  }
}
