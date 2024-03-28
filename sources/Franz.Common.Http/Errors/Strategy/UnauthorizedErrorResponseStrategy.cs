using Franz.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors.Strategy;

public class UnauthorizedErrorResponseStrategy : IErrorResponseStrategy
{
    int IErrorResponseStrategy.Order => 5;

    public int ResultStatusCode => StatusCodes.Status401Unauthorized;

    public bool IsWaitedErrorType(Exception exception)
    {
        var result = exception is UnauthorizedException;

        return result;
    }

    public string CreateMessage(ExceptionContext context)
    {
        var result = context.Exception.Message;

        return result;
    }
}
