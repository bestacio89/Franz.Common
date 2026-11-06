using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Estrategies;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Caching;

/// <summary>
/// Namespaces cache keys by the validated tenant identity of the current execution.
/// Register this strategy only when tenant-scoped cache access is required.
/// </summary>
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

    private string Prefix
    {
        get
        {
            var context = tenantContextAccessor.Current
                ?? throw new InvalidOperationException(
                    "A validated tenant execution context is required when tenant-scoped cache keys are enabled.");

            return $"tenant:{context.TenantId:N}:";
        }
    }
}
