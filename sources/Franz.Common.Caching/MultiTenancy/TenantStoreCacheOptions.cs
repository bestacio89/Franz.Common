namespace Franz.Common.Caching.MultiTenancy;

/// <summary>Controls the maximum age of cached tenant registry metadata.</summary>
public sealed class TenantStoreCacheOptions
{
    /// <summary>
    /// Absolute lifetime for tenant registry entries. Sliding expiration is deliberately
    /// not used so frequently accessed stale tenant metadata cannot live indefinitely.
    /// </summary>
    public TimeSpan AbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(1);
}
