using Franz.Common.Errors;
using Franz.Common.Http.Errors.Strategy;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors;

public interface IErrorResponseProvider
{
    public ErrorResponse PrepareResponse(ExceptionContext exceptionContext, IErrorResponseStrategy errorResponse);
}
