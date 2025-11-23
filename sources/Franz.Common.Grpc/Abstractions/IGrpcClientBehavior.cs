using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Defines the contract for a Franz gRPC client-side behavior.
/// Behaviors form a pipeline that wraps outgoing RPC calls.
/// </summary>
/// <typeparam name="TRequest">The request message type.</typeparam>
/// <typeparam name="TResponse">The response message type.</typeparam>
public interface IGrpcClientBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  /// <summary>
  /// Executes the behavior.
  /// Behaviors must call <paramref name="next"/> exactly once unless short-circuiting.
  /// </summary>
  /// <param name="request">The outgoing request message.</param>
  /// <param name="context">The logical call context (correlation, tenant, metadata...)</param>
  /// <param name="next">The next behavior or the final gRPC client call.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcClientPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default);
}

/// <summary>
/// Delegate representing the next behavior (or final transport call) in the pipeline.
/// </summary>
public delegate Task<TResponse> GrpcClientPipelineDelegate<TRequest, TResponse>(
    TRequest request,
    GrpcCallContext context,
    CancellationToken cancellationToken)
    where TRequest : class
    where TResponse : class;
