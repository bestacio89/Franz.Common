using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors; // adjust namespace if using .Server

/// <summary>
/// Resolves the tenant for the current gRPC server request,
/// inserts it into the GrpcCallContext, and passes execution forward.
/// </summary>
/// <typeparam name="TRequest">Incoming gRPC request type.</typeparam>
/// <typeparam name="TResponse">Outgoing gRPC response type.</typeparam>
public sealed class TenantResolutionClientBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzTenantResolver _tenantResolver;

  public TenantResolutionClientBehavior(IFranzTenantResolver tenantResolver)
  {
    _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
  }

  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    // Resolve tenant via your multi-tenancy engine
    var tenantId = await _tenantResolver
        .ResolveTenantAsync(context, cancellationToken)
        .ConfigureAwait(false);

    // Update context only if resolver returned a value
    if (!string.IsNullOrWhiteSpace(tenantId))
    {
      context = context.WithTenant(tenantId);
    }

    // Continue to next behavior
    return await next(request, context, cancellationToken)
        .ConfigureAwait(false);
  }
}
