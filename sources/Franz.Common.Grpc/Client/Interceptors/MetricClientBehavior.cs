using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors;

/// <summary>
/// Records outgoing gRPC request counts, success/failure counts, and execution duration.
/// Uses the Franz metrics abstraction so hosts can select their metrics backend.
/// </summary>
public sealed class MetricClientBehavior<TRequest, TResponse>
    : IGrpcClientBehavior<TRequest, TResponse>
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
      GrpcClientPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    _metrics.RecordRequest(context);
    var stopwatch = Stopwatch.StartNew();

    try
    {
      var response = await next(request, context, cancellationToken)
          .ConfigureAwait(false);

      stopwatch.Stop();
      _metrics.RecordSuccess(context, stopwatch.ElapsedMilliseconds);
      return response;
    }
    catch (Exception exception)
    {
      stopwatch.Stop();
      _metrics.RecordFailure(context, exception, stopwatch.ElapsedMilliseconds);
      throw;
    }
  }
}
