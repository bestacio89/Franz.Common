using Franz.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors.Strategy;

public class NotFoundErrorResponseStrategy : IErrorResponseStrategy
{
  public int Order => 3;

  public int ResultStatusCode => StatusCodes.Status404NotFound;

  public bool IsWaitedErrorType(Exception exception)
  {
    var result = exception is NotFoundException;

    return result;
  }

  public string CreateMessage(ExceptionContext context)
  {
    var result = context.Exception.Message;

    return result;
  }
}
