namespace Franz.Common.Messaging.MultiTenancy.Configuration;

/// <summary>Controls fail-closed tenant execution requirements for message consumers.</summary>
public sealed class TenantMessagingOptions
{
    /// <summary>
    /// Requires every hosted message consumer to establish a validated tenant execution scope.
    /// Enable this when tenant-filtered persistence is used by message handlers.
    /// </summary>
    public bool RequireTenantScope { get; set; }
}
