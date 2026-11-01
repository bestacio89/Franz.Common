namespace Franz.Common.MultiTenancy;

public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
