using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
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

    // 👇 You could inject a CorrelationIdAccessor if you want to grab it from HTTP context later
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

      // ✅ Use existing correlation ID if available (e.g. from HTTP headers), otherwise generate new
      var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
      CorrelationId.Current = correlationId; // make sure it's accessible globally for this scope

      var stopwatch = Stopwatch.StartNew();

      using (_logger.BeginScope(new { CorrelationId = correlationId }))
      {
        try
        {
          if (_env.IsDevelopment())
          {
            _logger.LogInformation(
              "[{Prefix}] {RequestName} [{CorrelationId}] started with payload {@Request}",
              prefix, requestName, correlationId, request);
          }
          else
          {
            _logger.LogInformation(
              "[{Prefix}] {RequestName} [{CorrelationId}] started",
              prefix, requestName, correlationId);
          }

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



}
