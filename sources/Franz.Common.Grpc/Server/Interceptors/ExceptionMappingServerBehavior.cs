using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

public sealed class ExceptionMappingServerBehavior<TRequest, TResponse>
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
      return await next(request, context, cancellationToken)
          .ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      var outcome = GrpcOutcome.FromException(ex);
      throw outcome.ToRpcException();
    }
  }
}
