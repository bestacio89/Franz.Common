using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors; // or .Server.Interceptors

/// <summary>
/// Records request counts, success/failure counts, and execution duration metrics.
/// Uses IFranzGrpcMetrics abstraction to support any backend (OTel, Prometheus, etc.).
/// </summary>
public sealed class MetricClientBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzGrpcMetrics _metrics;

  public MetricClientBehavior(IFranzGrpcMetrics metrics)
  {
    _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
  }

  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    // Record request arrival
    _metrics.RecordRequest(context);

    var sw = Stopwatch.StartNew();

    try
    {
      // Execute downstream behaviors
      var response = await next(request, context, cancellationToken)
          .ConfigureAwait(false);

      sw.Stop();

      // Record success metrics
      _metrics.RecordSuccess(context, sw.ElapsedMilliseconds);

      return response;
    }
    catch (Exception ex)
    {
      sw.Stop();

      // Record failure metrics
      _metrics.RecordFailure(context, ex, sw.ElapsedMilliseconds);

      throw;
    }
  }
}
