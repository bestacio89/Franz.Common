using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors; // or .Server.Interceptors

/// <summary>
/// Final interceptor in the server pipeline.
/// Converts all exceptions into RpcExceptions using the standardized GrpcOutcome mapping.
/// Ensures consistent error semantics across all gRPC services.
/// </summary>
public sealed class ExceptionMappingClientBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    try
    {
      // Let the rest of the pipeline execute normally
      return await next(request, context, cancellationToken)
          .ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      // Convert ANY exception to a well-structured GrpcOutcome
      var outcome = GrpcOutcome.FromException(ex);

      // Convert the outcome into an RpcException to let gRPC handle properly
      throw outcome.ToRpcException();
    }
  }
}
