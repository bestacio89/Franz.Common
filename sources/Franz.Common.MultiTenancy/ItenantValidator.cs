namespace Franz.Common.MultiTenancy;

/// <summary>Validates tenant eligibility before application execution.</summary>
public interface ITenantValidator
{
    Task<bool> IsValidAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
