using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Represents a server-side Franz gRPC behavior.
/// These behaviors wrap gRPC service method execution and form a pipeline.
/// </summary>
/// <typeparam name="TRequest">The incoming gRPC message type.</typeparam>
/// <typeparam name="TResponse">The outgoing gRPC response type.</typeparam>
public interface IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  /// <summary>
  /// Executes the behavior.
  /// Behaviors must call <paramref name="next"/> exactly once, unless
  /// intentionally short-circuiting (e.g., authorization failure).
  /// </summary>
  /// <param name="request">The request message from the client.</param>
  /// <param name="context">The Franz gRPC call context, built from ServerCallContext.</param>
  /// <param name="next">The next behavior (or the actual service method).</param>
  /// <param name="cancellationToken">Cancellation token for the RPC call.</param>
  Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the next behavior or the service method implementation.
/// Mirrors the client-side delegate.
/// </summary>
public delegate Task<TResponse> GrpcServerPipelineDelegate<TRequest, TResponse>(
    TRequest request,
    GrpcCallContext context,
    CancellationToken cancellationToken)
    where TRequest : class
    where TResponse : class;
