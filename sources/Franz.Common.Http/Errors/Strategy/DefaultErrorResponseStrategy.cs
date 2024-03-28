using Franz.Common.Http.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Http.Errors.Strategy;

public class DefaultErrorResponseStrategy : IErrorResponseStrategy
{
  private readonly IHostEnvironment hostEnvironment;

  public DefaultErrorResponseStrategy(IHostEnvironment hostEnvironment)
  {
    this.hostEnvironment = hostEnvironment;
  }

  public int Order => 100;

  public int ResultStatusCode => StatusCodes.Status500InternalServerError;

  public bool IsWaitedErrorType(Exception exception)
  {
    return true;
  }

  public string CreateMessage(ExceptionContext context)
  {
    var result = context.Exception.ToString();

    if (hostEnvironment.IsProduction())
      result = Resources.TechnicalErrorMessage;

    return result;
  }
}
