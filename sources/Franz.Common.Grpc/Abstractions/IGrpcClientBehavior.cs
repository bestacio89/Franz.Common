using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Defines a client-side gRPC behavior. Behaviors form a pipeline that wraps
/// outgoing RPC calls.
/// </summary>
/// <typeparam name="TRequest">The request message type.</typeparam>
/// <typeparam name="TResponse">The response message type.</typeparam>
public interface IGrpcClientBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Executes the behavior. Implementations must invoke <paramref name="next"/>
    /// exactly once unless they intentionally short-circuit the call.
    /// </summary>
    Task<TResponse> InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcClientPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default);
}
