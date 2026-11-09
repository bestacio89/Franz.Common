using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors;

/// <summary>
/// Normalizes exceptions raised by the outgoing client pipeline into gRPC outcomes.
/// Existing RpcException status and trailers are preserved by GrpcOutcome.FromException.
/// </summary>
public sealed class ExceptionMappingClientBehavior<TRequest, TResponse>
    : IGrpcClientBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcClientPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    try
    {
      return await next(request, context, cancellationToken)
          .ConfigureAwait(false);
    }
    catch (Exception exception)
    {
      throw GrpcOutcome.FromException(exception).ToRpcException();
    }
  }
}
