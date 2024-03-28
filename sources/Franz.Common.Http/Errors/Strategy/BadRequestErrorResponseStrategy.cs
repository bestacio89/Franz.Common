using Franz.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors.Strategy;

public class BadRequestErrorResponseStrategy : IErrorResponseStrategy
{
  public int Order => 1;

  public int ResultStatusCode => StatusCodes.Status400BadRequest;

  public bool IsWaitedErrorType(Exception exception)
  {
    var result = exception is FunctionalException;

    return result;
  }

  public string CreateMessage(ExceptionContext context)
  {
    var result = context.Exception.Message;

    return result;
  }
}
