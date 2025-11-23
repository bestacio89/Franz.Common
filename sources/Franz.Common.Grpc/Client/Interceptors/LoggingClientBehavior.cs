using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors; // or .Server.Interceptors

/// <summary>
/// Creates a structured log scope, logs incoming requests, outgoing responses,
/// failures, and timing metrics. Does not handle exceptions.
/// </summary>
public sealed class LoggingClientBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzGrpcLogger _logger;

  public LoggingClientBehavior(IFranzGrpcLogger logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    using var scope = _logger.BeginScope(context);
    var sw = Stopwatch.StartNew();

    // Log incoming request
    _logger.LogRequest(context, request);

    try
    {
      // Continue pipeline
      var response = await next(request, context, cancellationToken)
          .ConfigureAwait(false);

      sw.Stop();

      // Log success response
      _logger.LogResponse(context, response, sw.ElapsedMilliseconds);

      return response;
    }
    catch (Exception ex)
    {
      sw.Stop();

      // Log failure — but do NOT handle it.
      _logger.LogError(context, ex, sw.ElapsedMilliseconds);

      throw;
    }
  }
}
