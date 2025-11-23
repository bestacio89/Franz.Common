using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors; // or .Server.Interceptors

/// <summary>
/// Enforces authorization for incoming gRPC requests using the Franz authorization engine.
/// This behavior runs after Validation and TenantResolution.
/// </summary>
public sealed class AuthorizationClientBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
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
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    // Perform authorization — expected to throw if unauthorized.
    await _authorizationService
        .AuthorizeAsync(context, cancellationToken)
        .ConfigureAwait(false);

    // Proceed in pipeline
    return await next(request, context, cancellationToken)
        .ConfigureAwait(false);
  }
}
