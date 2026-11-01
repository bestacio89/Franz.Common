using Franz.Common.Grpc.Abstractions;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Grpc.Client.Interceptors;

/// <summary>
/// Propagates only the tenant from the validated execution context.
/// Caller-created tenant metadata is removed when no validated context exists.
/// </summary>
public sealed class TenantResolutionClientBehavior<TRequest, TResponse>
    : IGrpcClientBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly ITenantExecutionContextAccessor? _tenantContextAccessor;
    private readonly IFranzTenantResolver? _legacyTenantResolver;

    /// <summary>
    /// Creates a behavior that propagates only a validated tenant execution context.
    /// </summary>
    public TenantResolutionClientBehavior(ITenantExecutionContextAccessor? tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    /// <summary>
    /// Retains the resolver-based constructor for consumers compiled against earlier releases.
    /// </summary>
    public TenantResolutionClientBehavior(IFranzTenantResolver tenantResolver)
    {
        _legacyTenantResolver = tenantResolver
            ?? throw new ArgumentNullException(nameof(tenantResolver));
    }

    public Task<TResponse> InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcClientPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (_legacyTenantResolver is not null)
            return InvokeWithLegacyResolverAsync(request, context, next, cancellationToken);

        var tenantId = _tenantContextAccessor?.Current?.TenantId;
        var resolvedContext = tenantId is { } id && id != Guid.Empty
            ? context.WithTenant(id.ToString("D"))
            : context.WithTenant(null);

        return next(request, resolvedContext, cancellationToken);
    }

    private async Task<TResponse> InvokeWithLegacyResolverAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcClientPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var tenantId = await _legacyTenantResolver!
            .ResolveTenantAsync(context, cancellationToken)
            .ConfigureAwait(false);

        var resolvedContext = string.IsNullOrWhiteSpace(tenantId)
            ? context.WithTenant(null)
            : context.WithTenant(tenantId);

        return await next(request, resolvedContext, cancellationToken).ConfigureAwait(false);
    }
}
