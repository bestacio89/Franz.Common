using Franz.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors.Strategy;

public class PreconditionFailedErrorResponseStrategy : IErrorResponseStrategy
{
    public int Order => 4;

    public int ResultStatusCode => StatusCodes.Status412PreconditionFailed;

    public bool IsWaitedErrorType(Exception exception)
    {
        var result = exception is PreconditionFailedException;

        return result;
    }

    public string CreateMessage(ExceptionContext context)
    {
        var result = context.Exception.Message;

        return result;
    }
}
