using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

/// <summary>
/// Applies the configured authorization policy to every server-side streaming
/// RPC shape. Stream authorization begins when the stream is consumed.
/// </summary>
public sealed class AuthorizationStreamingBehavior<TRequest, TResponse> :
    IGrpcServerStreamingBehavior<TRequest, TResponse>,
    IGrpcClientStreamingBehavior<TRequest, TResponse>,
    IGrpcDuplexStreamingBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly IFranzAuthorizationService _authorization;

    public AuthorizationStreamingBehavior(IFranzAuthorizationService authorization)
    {
        _authorization = authorization ?? throw new System.ArgumentNullException(nameof(authorization));
    }

    public int Order => GrpcStreamingBehaviorOrder.Authorization;

    public async IAsyncEnumerable<TResponse> InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _authorization.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);

        await foreach (var response in next(request, context, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return response;
        }
    }

    public async Task<TResponse> InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcClientStreamingPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        await _authorization.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);

        return await next(requestStream, context, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<TResponse> InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _authorization.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);

        await foreach (var response in next(requestStream, context, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return response;
        }
    }
}
