using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors;

public sealed class TenantResolutionClientBehavior<TRequest, TResponse>(IFranzTenantResolver tenantResolver)
    : IGrpcClientBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    public async Task<TResponse> InvokeAsync(TRequest request, GrpcCallContext context,
        GrpcClientPipelineDelegate<TRequest, TResponse> next, CancellationToken cancellationToken = default)
    {
        var tenantId = await tenantResolver.ResolveTenantAsync(context, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(tenantId)) context = context.WithTenant(tenantId);
        return await next(request, context, cancellationToken).ConfigureAwait(false);
    }
}
