using Franz.Common.Http.Errors.Strategy;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors;

public class ExceptionFilter : IExceptionFilter
{
  private readonly IEnumerable<IErrorResponseStrategy> errorResponsesStrategies;
  private readonly IErrorResponseProvider errorResponseProvider;

  public ExceptionFilter(IEnumerable<IErrorResponseStrategy> errorResponsesStrategies, IErrorResponseProvider errorResponseProvider)
  {
    this.errorResponsesStrategies = errorResponsesStrategies;
    this.errorResponseProvider = errorResponseProvider;
  }

  public void OnException(ExceptionContext context)
  {
    var errorResponseStrategy = errorResponsesStrategies
                            .OrderBy(o => o.Order)
                            .First(e => e.IsWaitedErrorType(context.Exception));

    errorResponseProvider.PrepareResponse(context, errorResponseStrategy);
  }
}
