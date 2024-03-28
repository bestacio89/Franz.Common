using Franz.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors.Strategy;

public class ForbiddenErrorResponseStrategy : IErrorResponseStrategy
{
    public int Order => 2;

    public int ResultStatusCode => StatusCodes.Status403Forbidden;

    public bool IsWaitedErrorType(Exception exception)
    {
        var result = exception is ForbiddenException;

        return result;
    }

    public string CreateMessage(ExceptionContext context)
    {
        var result = context.Exception.Message;

        return result;
    }
}
