namespace Franz.Common.Http.MultiTenancy.Configuration;

/// <summary>Configures the opt-in claims-based tenant membership authorizer.</summary>
public sealed class TenantClaimsAuthorizationOptions
{
    /// <summary>
    /// Claim containing the tenant GUID. A claim is membership evidence only
    /// when its value parses to the exact resolved tenant identifier.
    /// </summary>
    public string TenantIdClaimType { get; set; } = "tenant_id";
}
