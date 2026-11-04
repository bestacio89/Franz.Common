namespace Franz.Common.Caching.MultiTenancy;

/// <summary>Explicit invalidation boundary for cached tenant registry metadata.</summary>
public interface ITenantStoreCacheInvalidator
{
    /// <summary>Invalidates a tenant and all host aliases observed for that tenant.</summary>
    void InvalidateTenant(Guid tenantId);

    /// <summary>Invalidates a canonical hostname lookup.</summary>
    void InvalidateHost(string host);

    /// <summary>Invalidates all tenant registry entries held by this cache instance.</summary>
    void InvalidateAll();
}
