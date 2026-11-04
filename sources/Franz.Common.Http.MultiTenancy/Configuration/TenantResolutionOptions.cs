namespace Franz.Common.Http.MultiTenancy.Configuration;

/// <summary>Controls tenant enforcement at the HTTP boundary.</summary>
public sealed class TenantResolutionOptions
{
    /// <summary>When true, requests without a valid active tenant are rejected.</summary>
    public bool RequireTenant { get; set; } = true;

    /// <summary>
    /// When true, authenticated requests require an application-registered tenant
    /// membership authorizer. The default preserves fail-closed authorization.
    /// </summary>
    public bool RequireAuthenticatedAuthorization { get; set; } = true;

    /// <summary>
    /// Request paths that bypass tenant resolution. Entries are path prefixes
    /// matched on segment boundaries, case-insensitively (for example, "/health").
    /// The application remains responsible for securing excluded endpoints.
    /// </summary>
    public IList<string> ExcludedPaths { get; } = new List<string>();
}
