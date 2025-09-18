using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting; // 👈 needed for IHostEnvironment
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Logging
{
  public class LoggingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  {
    private readonly ILogger<LoggingPipeline<TRequest, TResponse>> _logger;
    private readonly IHostEnvironment _env;

    public LoggingPipeline(
      ILogger<LoggingPipeline<TRequest, TResponse>> logger,
      IHostEnvironment env)
    {
      _logger = logger;
      _env = env;
    }

    public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
    {
      var requestName = request?.GetType().Name ?? typeof(TRequest).Name;

      var prefix = requestName.EndsWith("Query", StringComparison.OrdinalIgnoreCase)
        ? "Query"
        : requestName.EndsWith("Command", StringComparison.OrdinalIgnoreCase)
          ? "Command"
          : "Request";

      var correlationId = Guid.NewGuid().ToString("N");
      var stopwatch = Stopwatch.StartNew();

      if (_env.IsDevelopment())
      {
        // 🔥 Full Dev log
        _logger.LogInformation(
          "[{Prefix}] {RequestName} [{CorrelationId}] started with payload {@Request}",
          prefix, requestName, correlationId, request);
      }
      else
      {
        // 🟢 Lean Prod log
        _logger.LogInformation(
          "[{Prefix}] {RequestName} [{CorrelationId}] started",
          prefix, requestName, correlationId);
      }

      try
      {
        var response = await next();

        stopwatch.Stop();

        if (_env.IsDevelopment())
        {
          _logger.LogInformation(
            "[{Prefix}] {RequestName} [{CorrelationId}] finished in {Elapsed} ms with response {@Response}",
            prefix, requestName, correlationId, stopwatch.ElapsedMilliseconds, response);
        }
        else
        {
          _logger.LogInformation(
            "[{Prefix}] {RequestName} [{CorrelationId}] finished in {Elapsed} ms",
            prefix, requestName, correlationId, stopwatch.ElapsedMilliseconds);
        }

        return response;
      }
      catch (Exception ex)
      {
        stopwatch.Stop();

        if (_env.IsDevelopment())
        {
          _logger.LogError(ex,
            "[{Prefix}] {RequestName} [{CorrelationId}] failed after {Elapsed} ms",
            prefix, requestName, correlationId, stopwatch.ElapsedMilliseconds);
        }
        else
        {
          _logger.LogError(
            "[{Prefix}] {RequestName} [{CorrelationId}] failed after {Elapsed} ms with {ErrorMessage}",
            prefix, requestName, correlationId, stopwatch.ElapsedMilliseconds, ex.Message);
        }

        throw;
      }
    }
  }
}
