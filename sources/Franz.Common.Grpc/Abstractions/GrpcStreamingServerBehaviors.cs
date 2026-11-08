using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Intercepts a server-streaming RPC. The returned stream represents the complete
/// response lifetime; implementations must defer scoped work until enumeration
/// and release resources when enumeration completes, fails, or is disposed.
/// </summary>
public interface IGrpcServerStreamingBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    IAsyncEnumerable<TResponse> InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerStreamingPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Intercepts a client-streaming RPC. The task must cover the complete consumption
/// of the request stream and must observe cancellation while reading it.
/// </summary>
public interface IGrpcClientStreamingBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    Task<TResponse> InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcClientStreamingPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Intercepts a bidirectional streaming RPC. The returned stream represents the
/// complete response lifetime and must remain cancellation-aware and disposable.
/// </summary>
public interface IGrpcDuplexStreamingBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    IAsyncEnumerable<TResponse> InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default);
}

/// <summary>Next delegate for a server-streaming RPC.</summary>
public delegate IAsyncEnumerable<TResponse> GrpcServerStreamingPipelineDelegate<TRequest, TResponse>(
    TRequest request,
    GrpcCallContext context,
    CancellationToken cancellationToken)
    where TRequest : class
    where TResponse : class;

/// <summary>Next delegate for a client-streaming RPC.</summary>
public delegate Task<TResponse> GrpcClientStreamingPipelineDelegate<TRequest, TResponse>(
    IAsyncEnumerable<TRequest> requestStream,
    GrpcCallContext context,
    CancellationToken cancellationToken)
    where TRequest : class
    where TResponse : class;

/// <summary>Next delegate for a bidirectional streaming RPC.</summary>
public delegate IAsyncEnumerable<TResponse> GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse>(
    IAsyncEnumerable<TRequest> requestStream,
    GrpcCallContext context,
    CancellationToken cancellationToken)
    where TRequest : class
    where TResponse : class;
