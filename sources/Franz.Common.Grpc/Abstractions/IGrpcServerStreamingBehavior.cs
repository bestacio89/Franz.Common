using System.Collections.Generic;
using System.Threading;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Intercepts a server-streaming RPC. The returned stream represents the complete
/// response lifetime; implementations must defer scoped work until enumeration
/// and release resources when enumeration completes, fails, or is disposed.
/// </summary>
public interface IGrpcServerStreamingBehavior<TRequest, TResponse> : IOrderedGrpcStreamingBehavior
    where TRequest : class
    where TResponse : class
{
    IAsyncEnumerable<TResponse> InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerStreamingPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default);
}
