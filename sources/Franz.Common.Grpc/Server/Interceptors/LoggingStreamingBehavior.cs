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
/// Logs streaming RPC lifecycle events without enumerating or logging stream payloads.
/// The logger scope remains active until the complete stream lifetime ends.
/// </summary>
public sealed class LoggingStreamingBehavior<TRequest, TResponse> :
    IGrpcServerStreamingBehavior<TRequest, TResponse>,
    IGrpcClientStreamingBehavior<TRequest, TResponse>,
    IGrpcDuplexStreamingBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly IFranzGrpcLogger _logger;

    public LoggingStreamingBehavior(IFranzGrpcLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int Order => GrpcStreamingBehaviorOrder.Logging;

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
        using var scope = _logger.BeginScope(context);
        var stopwatch = Stopwatch.StartNew();
        const string shape = "client_streaming";
        _logger.LogStreamStarted(context, shape);

        try
        {
            var response = await next(requestStream, context, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogStreamCompleted(context, shape, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogStreamError(context, shape, exception, stopwatch.ElapsedMilliseconds);
            throw;
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
        using var scope = _logger.BeginScope(context);
        var stopwatch = Stopwatch.StartNew();
        _logger.LogStreamStarted(context, rpcShape);

        IAsyncEnumerator<TResponse>? enumerator = null;
        Exception? failure = null;
        Exception? disposalFailure = null;
        var completed = false;

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
                    completed = true;
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

            stopwatch.Stop();
            if (completed && failure is null)
                _logger.LogStreamCompleted(context, rpcShape, stopwatch.ElapsedMilliseconds);
            else if (failure is not null)
                _logger.LogStreamError(context, rpcShape, failure, stopwatch.ElapsedMilliseconds);
            else
                _logger.LogStreamAborted(context, rpcShape, stopwatch.ElapsedMilliseconds);

            if (disposalFailure is not null && ReferenceEquals(failure, disposalFailure))
                ExceptionDispatchInfo.Capture(disposalFailure).Throw();
        }
    }
}
