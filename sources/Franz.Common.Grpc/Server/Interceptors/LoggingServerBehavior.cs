using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

public sealed class LoggingServerBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzGrpcLogger _logger;

  public LoggingServerBehavior(IFranzGrpcLogger logger)
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

    _logger.LogRequest(context, request);

    try
    {
      var response = await next(request, context, cancellationToken)
          .ConfigureAwait(false);

      sw.Stop();
      _logger.LogResponse(context, response, sw.ElapsedMilliseconds);

      return response;
    }
    catch (Exception ex)
    {
      sw.Stop();
      _logger.LogError(context, ex, sw.ElapsedMilliseconds);
      throw;
    }
  }
}
