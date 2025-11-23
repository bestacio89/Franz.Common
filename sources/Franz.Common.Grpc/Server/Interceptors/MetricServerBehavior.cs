using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

public sealed class MetricServerBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzGrpcMetrics _metrics;

  public MetricServerBehavior(IFranzGrpcMetrics metrics)
  {
    _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
  }

  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    _metrics.RecordRequest(context);

    var sw = Stopwatch.StartNew();

    try
    {
      var response = await next(request, context, cancellationToken)
          .ConfigureAwait(false);

      sw.Stop();
      _metrics.RecordSuccess(context, sw.ElapsedMilliseconds);

      return response;
    }
    catch (Exception ex)
    {
      sw.Stop();
      _metrics.RecordFailure(context, ex, sw.ElapsedMilliseconds);
      throw;
    }
  }
}
