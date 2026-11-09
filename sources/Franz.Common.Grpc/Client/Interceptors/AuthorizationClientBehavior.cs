using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors;

/// <summary>
/// Applies the configured authorization policy before an outgoing gRPC call.
/// The host supplies the authorization service and its policy context.
/// </summary>
public sealed class AuthorizationClientBehavior<TRequest, TResponse>
    : IGrpcClientBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzAuthorizationService _authorizationService;

  public AuthorizationClientBehavior(IFranzAuthorizationService authorizationService)
  {
    _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
  }

  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcClientPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    await _authorizationService
        .AuthorizeAsync(context, cancellationToken)
        .ConfigureAwait(false);

    return await next(request, context, cancellationToken)
        .ConfigureAwait(false);
  }
}
