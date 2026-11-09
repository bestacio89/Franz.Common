using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

/// <summary>Records bounded-cardinality server call metrics and standard .NET diagnostics.</summary>
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

    public int Order => GrpcServerBehaviorOrder.Metrics;

    public async Task<TResponse> InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        var metricContext = GrpcMetricContext.Create(context, "unary", "server");
        _metrics.RecordRequest(metricContext);
        var stopwatch = Stopwatch.StartNew();
        using var activity = FranzGrpcDiagnostics.StartActivity(metricContext);

        var succeeded = false;
        Exception? failure = null;

        try
        {
            var response = await next(request, context, cancellationToken).ConfigureAwait(false);
            succeeded = true;
            return response;
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (succeeded)
                _metrics.RecordSuccess(metricContext, stopwatch.ElapsedMilliseconds);
            else
                _metrics.RecordFailure(metricContext, failure!, stopwatch.ElapsedMilliseconds);

            FranzGrpcDiagnostics.RecordCompleted(metricContext, succeeded, stopwatch.Elapsed.TotalSeconds);
            FranzGrpcDiagnostics.SetActivityOutcome(activity, succeeded, failure);
        }
    }
}
