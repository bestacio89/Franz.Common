using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

/// <summary>
/// Records metrics for all server-side streaming RPC shapes. Instrumentation spans
/// deferred enumeration and request consumption and uses bounded metric dimensions.
/// </summary>
public sealed class MetricStreamingBehavior<TRequest, TResponse> :
    IGrpcServerStreamingBehavior<TRequest, TResponse>,
    IGrpcClientStreamingBehavior<TRequest, TResponse>,
    IGrpcDuplexStreamingBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly IFranzGrpcMetrics _metrics;

    public MetricStreamingBehavior(IFranzGrpcMetrics metrics)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public int Order => GrpcStreamingBehaviorOrder.Metrics;

    async IAsyncEnumerable<TResponse> IGrpcServerStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var response in ObserveStream(
            () => next(request, context, cancellationToken),
            context,
            "server_streaming",
            cancellationToken).ConfigureAwait(false))
        {
            yield return response;
        }
    }

    async Task<TResponse> IGrpcClientStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcClientStreamingPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var metricContext = GrpcMetricContext.Create(context, "client_streaming", "server");
        _metrics.RecordRequest(metricContext);
        var stopwatch = Stopwatch.StartNew();
        using var activity = FranzGrpcDiagnostics.StartActivity(metricContext);
        var succeeded = false;
        Exception? failure = null;

        try
        {
            var response = await next(requestStream, context, cancellationToken).ConfigureAwait(false);
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
            Complete(metricContext, stopwatch, activity, succeeded, failure);
        }
    }

    async IAsyncEnumerable<TResponse> IGrpcDuplexStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var response in ObserveStream(
            () => next(requestStream, context, cancellationToken),
            context,
            "duplex_streaming",
            cancellationToken).ConfigureAwait(false))
        {
            yield return response;
        }
    }

    private async IAsyncEnumerable<TResponse> ObserveStream(
        Func<IAsyncEnumerable<TResponse>> next,
        GrpcCallContext context,
        string rpcShape,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var metricContext = GrpcMetricContext.Create(context, rpcShape, "server");
        _metrics.RecordRequest(metricContext);
        var stopwatch = Stopwatch.StartNew();
        using var activity = FranzGrpcDiagnostics.StartActivity(metricContext);

        IAsyncEnumerator<TResponse>? enumerator = null;
        Exception? failure = null;
        Exception? disposalFailure = null;
        var succeeded = false;

        try
        {
            try
            {
                enumerator = next().GetAsyncEnumerator(cancellationToken);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    failure = exception;
                    throw;
                }

                if (!hasNext)
                {
                    succeeded = true;
                    break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            try
            {
                if (enumerator is not null)
                    await enumerator.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                disposalFailure = exception;
                failure ??= exception;
            }

            if (disposalFailure is not null)
                succeeded = false;

            if (!succeeded && failure is null)
                failure = new OperationCanceledException("The response stream ended before normal completion.");

            Complete(metricContext, stopwatch, activity, succeeded, failure);

            if (disposalFailure is not null && ReferenceEquals(failure, disposalFailure))
                ExceptionDispatchInfo.Capture(disposalFailure).Throw();
        }
    }

    private void Complete(
        GrpcMetricContext context,
        Stopwatch stopwatch,
        Activity? activity,
        bool succeeded,
        Exception? failure)
    {
        stopwatch.Stop();

        if (succeeded)
            _metrics.RecordSuccess(context, stopwatch.ElapsedMilliseconds);
        else
            _metrics.RecordFailure(context, failure!, stopwatch.ElapsedMilliseconds);

        FranzGrpcDiagnostics.RecordCompleted(context, succeeded, stopwatch.Elapsed.TotalSeconds);
        FranzGrpcDiagnostics.SetActivityOutcome(activity, succeeded, failure);
    }
}
