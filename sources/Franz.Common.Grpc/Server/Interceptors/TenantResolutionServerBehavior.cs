using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

public sealed class TenantResolutionServerBehavior<TRequest, TResponse>(IFranzTenantResolver tenantResolver)
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    public async Task<TResponse> InvokeAsync(TRequest request, GrpcCallContext context,
        GrpcServerPipelineDelegate<TRequest, TResponse> next, CancellationToken cancellationToken = default)
    {
        // Remove caller-controlled tenant metadata before resolving trusted server-side identity.
        context = context.WithTenant(string.Empty);
        var tenantId = await tenantResolver.ResolveTenantAsync(context, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(tenantId)) context = context.WithTenant(tenantId);
        return await next(request, context, cancellationToken).ConfigureAwait(false);
    }
}
