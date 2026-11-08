using System.Collections.Generic;
using System.Threading;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Intercepts a bidirectional streaming RPC. The returned stream represents the
/// complete response lifetime and must remain cancellation-aware and disposable.
/// </summary>
public interface IGrpcDuplexStreamingBehavior<TRequest, TResponse> : IOrderedGrpcStreamingBehavior
    where TRequest : class
    where TResponse : class
{
    IAsyncEnumerable<TResponse> InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default);
}
