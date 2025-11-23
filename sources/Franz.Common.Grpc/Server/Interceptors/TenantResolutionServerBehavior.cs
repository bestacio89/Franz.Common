using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

public sealed class TenantResolutionServerBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzTenantResolver _tenantResolver;

  public TenantResolutionServerBehavior(IFranzTenantResolver tenantResolver)
  {
    _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
  }

  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    var tenantId = await _tenantResolver.ResolveTenantAsync(context, cancellationToken)
        .ConfigureAwait(false);

    if (!string.IsNullOrWhiteSpace(tenantId))
      context = context.WithTenant(tenantId);

    return await next(request, context, cancellationToken)
        .ConfigureAwait(false);
  }
}
