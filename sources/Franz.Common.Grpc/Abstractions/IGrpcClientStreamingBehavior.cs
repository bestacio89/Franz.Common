using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Intercepts a client-streaming RPC. The task must cover the complete consumption
/// of the request stream and must observe cancellation while reading it.
/// </summary>
public interface IGrpcClientStreamingBehavior<TRequest, TResponse> : IOrderedGrpcStreamingBehavior
    where TRequest : class
    where TResponse : class
{
    Task<TResponse> InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcClientStreamingPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default);
}
