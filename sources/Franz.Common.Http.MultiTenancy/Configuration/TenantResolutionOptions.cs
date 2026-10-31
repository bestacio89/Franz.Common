namespace Franz.Common.Http.MultiTenancy.Configuration;

/// <summary>Controls tenant enforcement at the HTTP boundary.</summary>
public sealed class TenantResolutionOptions
{
    /// <summary>When true, requests without a valid active tenant are rejected.</summary>
    public bool RequireTenant { get; set; } = true;
}
