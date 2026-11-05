namespace Franz.Common.MultiTenancy;

/// <summary>Validates tenant lifecycle and application-specific eligibility.</summary>
public interface ITenantValidator
{
    Task<bool> IsValidAsync(Guid tenantId, CancellationToken cancellationToken);
}
