namespace Franz.Common.Messaging.MultiTenancy.Configuration;

/// <summary>Controls fail-closed tenant execution requirements for message consumers.</summary>
public sealed class TenantMessagingOptions
{
    /// <summary>
    /// Requires hosted message consumers to establish a validated tenant execution scope.
    /// </summary>
    /// <remarks>
    /// Disabled by default because messaging tenancy services are also used for outbound metadata
    /// propagation. Enable this when consumers process tenant-scoped data.
    /// </remarks>
    public bool RequireTenantScope { get; set; }
}
