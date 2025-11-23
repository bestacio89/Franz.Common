using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

public sealed class AuthorizationServerBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzAuthorizationService _authorization;

  public AuthorizationServerBehavior(IFranzAuthorizationService authorization)
  {
    _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
  }

  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    await _authorization.AuthorizeAsync(context, cancellationToken)
        .ConfigureAwait(false);

    return await next(request, context, cancellationToken)
        .ConfigureAwait(false);
  }
}
