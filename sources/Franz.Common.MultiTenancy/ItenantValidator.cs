namespace Franz.Common.MultiTenancy;

public interface ITenantValidator
{
    Task<bool> IsValidAsync(Guid tenantId);

    Task<bool> IsValidAsync(Guid tenantId, CancellationToken cancellationToken)
        => IsValidAsync(tenantId);
}
