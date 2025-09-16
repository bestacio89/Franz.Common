using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Logging;
public class LoggingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
  private readonly ILogger<LoggingPipeline<TRequest, TResponse>> _logger;

  public LoggingPipeline(ILogger<LoggingPipeline<TRequest, TResponse>> logger)
  {
    _logger = logger;
  }

  public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
  {
    var requestName = typeof(TRequest).Name;
    _logger.LogInformation("Starting {RequestName}", requestName);

    var start = DateTime.UtcNow;
    try
    {
      var response = await next();
      var duration = DateTime.UtcNow - start;
      _logger.LogInformation("Finished {RequestName} in {Duration}ms", requestName, duration.TotalMilliseconds);
      return response;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error handling {RequestName}", requestName);
      throw;
    }
  }
}
