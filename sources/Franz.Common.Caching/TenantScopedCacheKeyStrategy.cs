using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Estrategies;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Caching;

/// <summary>Namespaces cache keys by validated tenant identity to prevent cross-tenant cache collisions.</summary>
public sealed class TenantScopedCacheKeyStrategy(
    DefaultCacheKeyStrategy inner,
    ITenantExecutionContextAccessor tenantContextAccessor) : ICacheKeyStrategy
{
    public string GetKey(object request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Prefix + inner.GetKey(request);
    }

    public string BuildKey<TRequest>(TRequest request)
        => Prefix + inner.BuildKey(request);

    private string Prefix => tenantContextAccessor.Current is { } context
        ? $"tenant:{context.TenantId:N}:"
        : "global:";
}
