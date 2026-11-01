using Franz.Common.Grpc.Abstractions;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Grpc.Client.Interceptors;

/// <summary>
/// Propagates only the tenant from the validated execution context.
/// Caller-created tenant metadata is removed when no validated context exists.
/// </summary>
public sealed class TenantResolutionClientBehavior<TRequest, TResponse>(
    ITenantExecutionContextAccessor? tenantContextAccessor)
    : IGrpcClientBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    public Task<TResponse> InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcClientPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tenantId = tenantContextAccessor?.Current?.TenantId;
        var resolvedContext = tenantId is { } id && id != Guid.Empty
            ? context.WithTenant(id.ToString("D"))
            : context.WithTenant(null);

        return next(request, resolvedContext, cancellationToken);
    }
}
