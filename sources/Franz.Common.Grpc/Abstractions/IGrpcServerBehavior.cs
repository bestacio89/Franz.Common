using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Defines a server-side gRPC behavior. Behaviors wrap service method execution
/// and form the unary server pipeline.
/// </summary>
/// <typeparam name="TRequest">The incoming gRPC message type.</typeparam>
/// <typeparam name="TResponse">The outgoing gRPC response type.</typeparam>
public interface IGrpcServerBehavior<TRequest, TResponse> : IOrderedGrpcServerBehavior
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
        GrpcServerPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default);
}
