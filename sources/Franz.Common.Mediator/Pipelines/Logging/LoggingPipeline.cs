#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Franz.Common.Mediator.Pipelines.Logging;

/// <summary>
/// Hardened logging pipeline for Mediator requests.
/// Enforces native Guid v7 correlation and detailed performance tracking.
/// </summary>
public sealed class LoggingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
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

    // If an ID exists (from an upstream HTTP header or Message), it is preserved.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();
    var stopwatch = Stopwatch.StartNew();

    // Scope the native Guid—Serilog will index this as a UUID, not a string.
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