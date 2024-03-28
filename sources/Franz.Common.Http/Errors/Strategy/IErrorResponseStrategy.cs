using Franz.Common.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Errors.Strategy;

public interface IErrorResponseStrategy : IScopedDependency
{
  public int Order { get; }

  public int ResultStatusCode { get; }

  public bool IsWaitedErrorType(Exception exception);

  public string CreateMessage(ExceptionContext context);
}
