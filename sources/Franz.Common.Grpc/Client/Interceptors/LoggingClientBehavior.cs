using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors;

/// <summary>
/// Logs outgoing gRPC requests, responses, failures, and elapsed time.
/// Does not handle exceptions.
/// </summary>
public sealed class LoggingClientBehavior<TRequest, TResponse>
    : IGrpcClientBehavior<TRequest, TResponse>
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
      GrpcClientPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    using var scope = _logger.BeginScope(context);
    var stopwatch = Stopwatch.StartNew();
    _logger.LogRequest(context, request);

    try
    {
      var response = await next(request, context, cancellationToken)
          .ConfigureAwait(false);

      stopwatch.Stop();
      _logger.LogResponse(context, response, stopwatch.ElapsedMilliseconds);
      return response;
    }
    catch (Exception exception)
    {
      stopwatch.Stop();
      _logger.LogError(context, exception, stopwatch.ElapsedMilliseconds);
      throw;
    }
  }
}
