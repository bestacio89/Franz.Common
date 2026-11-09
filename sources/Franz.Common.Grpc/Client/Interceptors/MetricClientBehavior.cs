using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors;

/// <summary>
/// Records outgoing unary calls through the host metrics abstraction and the
/// standard .NET Meter and ActivitySource diagnostics.
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
        var metricContext = GrpcMetricContext.Create(context, "unary", "client");
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
