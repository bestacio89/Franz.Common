using Franz.Common.Errors;
using Franz.Common.Http.Errors.Strategy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Http.Errors;

public class ErrorResponseProvider : IErrorResponseProvider
{
  private readonly IHostEnvironment hostEnvironment;

  public ErrorResponseProvider(IHostEnvironment hostEnvironment)
  {
    this.hostEnvironment = hostEnvironment;
  }

  public ErrorResponse PrepareResponse(ExceptionContext exceptionContext, IErrorResponseStrategy errorResponse)
  {
    var result = CreateResponse(exceptionContext);
    result.Message = exceptionContext.Exception.Message;
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
  {
    var result = new ObjectResult(errorResponse)
    {
      StatusCode = statusCode,
      DeclaredType = typeof(ErrorResponse),
    };

    return result;
  }
}
