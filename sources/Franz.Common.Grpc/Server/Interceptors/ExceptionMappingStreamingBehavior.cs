using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;
using Grpc.Core;

namespace Franz.Common.Grpc.Server.Interceptors;

/// <summary>
/// Maps failures from the complete lifetime of server-side streaming RPCs to
/// sanitized gRPC outcomes. It is registered as the outermost streaming behavior.
/// </summary>
public sealed class ExceptionMappingStreamingBehavior<TRequest, TResponse> :
    IGrpcServerStreamingBehavior<TRequest, TResponse>,
    IGrpcClientStreamingBehavior<TRequest, TResponse>,
    IGrpcDuplexStreamingBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    public int Order => GrpcStreamingBehaviorOrder.ExceptionMapping;

    async IAsyncEnumerable<TResponse> IGrpcServerStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var responses = InvokeServerStreamingAsync(request, context, next, cancellationToken);
        await foreach (var response in MapStreamAsync(responses, context, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
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
        try
        {
            return await next(requestStream, context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw Map(exception, context, cancellationToken);
        }
    }

    async IAsyncEnumerable<TResponse> IGrpcDuplexStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var responses = InvokeDuplexStreamingAsync(requestStream, context, next, cancellationToken);
        await foreach (var response in MapStreamAsync(responses, context, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return response;
        }
    }

    private static async IAsyncEnumerable<TResponse> InvokeServerStreamingAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var response in next(request, context, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return response;
        }
    }

    private static async IAsyncEnumerable<TResponse> InvokeDuplexStreamingAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var response in next(requestStream, context, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return response;
        }
    }

    private static async IAsyncEnumerable<TResponse> MapStreamAsync(
        IAsyncEnumerable<TResponse> responses,
        GrpcCallContext context,
        CancellationToken cancellationToken,
        [EnumeratorCancellation] CancellationToken enumerationToken = default)
    {
        var effectiveToken = enumerationToken.CanBeCanceled ? enumerationToken : cancellationToken;
        var enumerator = responses.GetAsyncEnumerator(effectiveToken);

        try
        {
            while (true)
            {
                bool hasNext;
                TResponse? current = null;

                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                    if (hasNext)
                        current = enumerator.Current;
                }
                catch (Exception exception)
                {
                    throw Map(exception, context, effectiveToken);
                }

                if (!hasNext)
                    yield break;

                yield return current!;
            }
        }
        finally
        {
            try
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw Map(exception, context, effectiveToken);
            }
        }
    }

    private static RpcException Map(
        Exception exception,
        GrpcCallContext context,
        CancellationToken cancellationToken)
    {
        var outcome = GrpcOutcome.FromException(
            exception,
            cancellationToken,
            context.DeadlineUtc,
            context.CorrelationId);

        return outcome.ToRpcException();
    }
}
