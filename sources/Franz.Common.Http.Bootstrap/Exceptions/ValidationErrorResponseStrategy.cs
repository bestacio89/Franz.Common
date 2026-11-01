using Franz.Common.Http.Errors.Strategy;
using Franz.Common.Mediator.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Bootstrap.Exceptions;

/// <summary>Maps mediator validation failures to client-correctable HTTP 400 responses.</summary>
public sealed class ValidationErrorResponseStrategy : IErrorResponseStrategy
{
    public int Order => 0;
    public int ResultStatusCode => StatusCodes.Status400BadRequest;
    public bool IsWaitedErrorType(Exception exception) => exception is ValidationException;
    public string CreateMessage(ExceptionContext context) => context.Exception.Message;
}
