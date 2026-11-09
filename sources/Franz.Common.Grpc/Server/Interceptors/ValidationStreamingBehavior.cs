using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

/// <summary>
/// Validates the unary request of a server-streaming call and validates every
/// request message as it is consumed by client-streaming and duplex handlers.
/// Validation remains lazy and cancellation-aware for streamed requests.
/// </summary>
public sealed class ValidationStreamingBehavior<TRequest, TResponse> :
    IGrpcServerStreamingBehavior<TRequest, TResponse>,
    IGrpcClientStreamingBehavior<TRequest, TResponse>,
    IGrpcDuplexStreamingBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly IFranzValidationEngine _validationEngine;

    public ValidationStreamingBehavior(IFranzValidationEngine validationEngine)
    {
        _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
    }

    public int Order => GrpcStreamingBehaviorOrder.Validation;

    async IAsyncEnumerable<TResponse> IGrpcServerStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _validationEngine
            .ValidateAsync(request, context.Method, cancellationToken)
            .ConfigureAwait(false);

        await foreach (var response in next(request, context, cancellationToken)
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
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var validatedRequests = ValidateRequestStream(requestStream, context.Method, cancellationToken);
        return await next(validatedRequests, context, cancellationToken).ConfigureAwait(false);
    }

    async IAsyncEnumerable<TResponse> IGrpcDuplexStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var validatedRequests = ValidateRequestStream(requestStream, context.Method, cancellationToken);
        await foreach (var response in next(validatedRequests, context, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return response;
        }
    }

    private async IAsyncEnumerable<TRequest> ValidateRequestStream(
        IAsyncEnumerable<TRequest> requestStream,
        string ruleSet,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var request in requestStream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            ArgumentNullException.ThrowIfNull(request);
            await _validationEngine
                .ValidateAsync(request, ruleSet, cancellationToken)
                .ConfigureAwait(false);

            yield return request;
        }
    }
}
