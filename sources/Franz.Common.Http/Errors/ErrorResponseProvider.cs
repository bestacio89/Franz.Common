using Franz.Common.Errors;
using Franz.Common.Http.Errors.Strategy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Http.Errors;

public sealed class ErrorResponseProvider(IHostEnvironment hostEnvironment) : IErrorResponseProvider
{
    public ErrorResponse PrepareResponse(ExceptionContext exceptionContext, IErrorResponseStrategy errorResponse)
    {
        ArgumentNullException.ThrowIfNull(exceptionContext);
        ArgumentNullException.ThrowIfNull(errorResponse);

        var result = CreateResponse(exceptionContext);
        result.Message = errorResponse.CreateMessage(exceptionContext);
        exceptionContext.Result = CreateContextResult(result, errorResponse.ResultStatusCode);

        return result;
    }

    private ErrorResponse CreateResponse(ExceptionContext context)
    {
        var response = new ErrorResponse();
        if (!hostEnvironment.IsProduction())
            response.StackTrace = context.Exception.StackTrace;
        return response;
    }

    public static ObjectResult CreateContextResult(ErrorResponse errorResponse, int statusCode)
        => new(errorResponse)
        {
            StatusCode = statusCode,
            DeclaredType = typeof(ErrorResponse)
        };
}
