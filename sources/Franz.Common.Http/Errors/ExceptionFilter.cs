using Franz.Common.Http.Errors.Strategy;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors;

public sealed class ExceptionFilter(
    IEnumerable<IErrorResponseStrategy> errorResponsesStrategies,
    IErrorResponseProvider errorResponseProvider) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var strategy = errorResponsesStrategies
            .OrderBy(candidate => candidate.Order)
            .FirstOrDefault(candidate => candidate.IsWaitedErrorType(context.Exception));

        if (strategy is null)
            throw new InvalidOperationException(
                "No HTTP error response strategy is registered. Register DefaultErrorResponseStrategy as the fallback.");

        errorResponseProvider.PrepareResponse(context, strategy);
    }
}
