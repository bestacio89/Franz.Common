using Franz.Common.Errors;
using Franz.Common.Http.Errors.Strategy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors.Strategy;

/// <summary>Maps explicit domain business failures to HTTP 422 responses.</summary>
public sealed class BusinessErrorResponseStrategy : IErrorResponseStrategy
{
    public int Order => 6;
    public int ResultStatusCode => StatusCodes.Status422UnprocessableEntity;
    public bool IsWaitedErrorType(Exception exception) => exception is BusinessException;
    public string CreateMessage(ExceptionContext context) => context.Exception.Message;
}
