namespace Franz.Common.MultiTenancy;

/// <summary>
/// Declares whether an application uses tenant isolation.
/// This is independent of the database routing/isolation mode.
/// </summary>
public sealed class MultiTenancyOptions
{
    /// <summary>
    /// Enables tenant isolation policies. Defaults to false so non-multi-tenant
    /// applications do not acquire tenant requirements by configuring persistence.
    /// </summary>
    public bool Enabled { get; set; }
}
